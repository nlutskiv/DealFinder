using System.Globalization;
using System.Text.RegularExpressions;
using CsvHelper;
using CsvHelper.Configuration;
using DealFinder.Api.Data;
using DealFinder.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DealFinder.Api.Services.Feed;

public class AwinFeedIngestionService : IAwinFeedIngestionService
{
    private const int BatchSize = 300;

    private const string Gpu = "gpu";
    private const string Cpu = "cpu";
    private const string Ram = "ram";
    private const string Psu = "psu";
    private const string Motherboard = "motherboard";
    private const string Storage = "storage";
    private const string Cooler = "cooler";
    private const string Case = "case";

    private readonly IDbContextFactory<DealFinderDbContext> _dbContextFactory;
    private readonly ILogger<AwinFeedIngestionService> _logger;

    public AwinFeedIngestionService(
        IDbContextFactory<DealFinderDbContext> dbContextFactory,
        ILogger<AwinFeedIngestionService> logger)
    {
        _dbContextFactory = dbContextFactory;
        _logger = logger;
    }

    public async Task IngestAsync(
        Stream feedStream,
        string sourceName,
        CancellationToken ct = default)
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            MissingFieldFound = null,
            BadDataFound = null,
            HeaderValidated = null,
            DetectDelimiter = true,
            TrimOptions = TrimOptions.Trim,
            PrepareHeaderForMatch = args =>
                args.Header
                    .Trim()
                    .Replace(" ", "_")
                    .Replace("-", "_")
                    .ToLowerInvariant()
        };

        var total = 0;
        var imported = 0;
        var skipped = 0;

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

        // Preload both tables once. Supabase round-trips are ~150ms, so per-row lookups
        // dominate sync time — two upfront queries replace two SELECTs per imported row.
        var catalogCache = new Dictionary<(string Category, string Label), ProductCatalog>();
        foreach (var product in await dbContext.ProductCatalog.ToListAsync(ct))
        {
            catalogCache.TryAdd((product.Category, product.Label), product);
        }

        var listingCache = new Dictionary<string, ProductListing>();
        foreach (var existingListing in await dbContext.ProductListings.ToListAsync(ct))
        {
            listingCache.TryAdd(existingListing.Url, existingListing);
        }

        using var reader = new StreamReader(feedStream);
        using var csv = new CsvReader(reader, config);

        await csv.ReadAsync();
        csv.ReadHeader();

        while (await csv.ReadAsync())
        {
            ct.ThrowIfCancellationRequested();
            total++;

            var row = ReadRow(csv);

            if (!TryBuildListing(row, sourceName, out var catalogLabel, out var catalogDescription, out var listing))
            {
                skipped++;
                continue;
            }

            var catalogProduct = await GetOrCreateCatalogProductAsync(
                dbContext,
                catalogCache,
                listing.Category,
                catalogLabel,
                catalogDescription,
                ct
            );

            listing.ProductCatalogId = catalogProduct.Id;

            await UpsertListingAsync(dbContext, listingCache, listing, ct);
            imported++;

            if (imported % BatchSize == 0)
            {
                await dbContext.SaveChangesAsync(ct);

                _logger.LogInformation(
                    "Awin ingest progress. Source={Source}, Total={Total}, Imported={Imported}, Skipped={Skipped}",
                    sourceName,
                    total,
                    imported,
                    skipped
                );
            }
        }

        await dbContext.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Awin ingest complete. Source={Source}, Total={Total}, Imported={Imported}, Skipped={Skipped}",
            sourceName,
            total,
            imported,
            skipped
        );
    }

    private static AwinRow ReadRow(CsvReader csv)
    {
        return new AwinRow
        {
            ProductName = Field(csv, "product_name"),
            Description = Field(csv, "description"),
            ShortDescription = Field(csv, "product_short_description"),
            Specifications = Field(csv, "specifications"),

            SearchPrice = Field(csv, "search_price"),
            DeliveryCost = Field(csv, "delivery_cost"),
            Currency = Field(csv, "currency"),

            MerchantName = Field(csv, "merchant_name"),
            MerchantCategory = Field(csv, "merchant_category"),
            CategoryName = Field(csv, "category_name"),
            CategoryPath = Field(csv, "merchant_product_category_path"),
            SecondCategory = Field(csv, "merchant_product_second_category"),
            ThirdCategory = Field(csv, "merchant_product_third_category"),
            ProductType = Field(csv, "product_type"),

            BrandName = Field(csv, "brand_name"),
            ProductModel = Field(csv, "product_model"),
            ModelNumber = Field(csv, "model_number"),
            Keywords = Field(csv, "keywords"),

            Condition = Field(csv, "condition"),
            InStock = Field(csv, "in_stock"),
            StockStatus = Field(csv, "stock_status"),
            StockQuantity = Field(csv, "stock_quantity"),
            IsForSale = Field(csv, "is_for_sale"),

            AwinDeepLink = Field(csv, "aw_deep_link"),
            MerchantDeepLink = Field(csv, "merchant_deep_link"),

            MerchantImageUrl = Field(csv, "merchant_image_url"),
            LargeImage = Field(csv, "large_image"),
            AwinImageUrl = Field(csv, "aw_image_url"),
            MerchantThumbUrl = Field(csv, "merchant_thumb_url")
        };
    }

    private static string Field(CsvReader csv, string name)
    {
        try
        {
            return csv.GetField(name)?.Trim() ?? "";
        }
        catch
        {
            return "";
        }
    }
    private static bool IsBadImage(string imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            return true;
        }

        var text = imageUrl.ToLowerInvariant();

        return ContainsAny(text, [
            "coming-soon",
            "coming_soon",
            "comingsoon",
            "no-image",
            "no_image",
            "placeholder",
            "image-not-available",
            "notavailable"
        ]);
    }

    private bool TryBuildListing(
        AwinRow row,
        string sourceName,
        out string catalogLabel,
        out string catalogDescription,
        out ProductListing listing)
    {
        catalogLabel = "";
        catalogDescription = "";
        listing = new ProductListing();

        if (string.IsNullOrWhiteSpace(row.ProductName))
        {
            return false;
        }

        var affiliateUrl = FirstNonEmpty(row.AwinDeepLink, row.MerchantDeepLink);

        if (string.IsNullOrWhiteSpace(affiliateUrl))
        {
            return false;
        }

        if (!IsAvailable(row))
        {
            return false;
        }

        if (!TryParseDecimal(row.SearchPrice, out var price) || price <= 0)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(row.Currency) &&
            !row.Currency.Equals("GBP", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var category = ClassifyCategory(row);

        if (category is null)
        {
            return false;
        }

        catalogLabel = ExtractCatalogLabel(category, row);
        catalogLabel = CanonicalProductLabel(category, catalogLabel);

        if (string.IsNullOrWhiteSpace(catalogLabel))
        {
            return false;
        }

        catalogDescription = BuildCatalogDescription(category, catalogLabel);

        var description = FirstNonEmpty(
            row.Description,
            row.ShortDescription,
            row.Specifications,
            row.ProductName
        );

        var imageUrl = FirstNonEmpty(
            row.MerchantImageUrl,
            row.LargeImage,
            row.AwinImageUrl,
            row.MerchantThumbUrl
        );
        if (IsBadImage(imageUrl))
        {
            return false;
        }



        var shipping = TryParseDecimal(row.DeliveryCost, out var parsedShipping)
            ? parsedShipping
            : 0m;

        listing = new ProductListing
        {
            Title = row.ProductName,
            Description = description,
            Price = Math.Round(price, 2),
            ShippingPrice = Math.Round(shipping, 2),
            Condition = NormaliseCondition(row),
            Marketplace = FirstNonEmpty(row.MerchantName, sourceName),
            Url = affiliateUrl,
            ImageUrl = imageUrl,
            Category = category,

        };

        return true;
    }

    private static string? ClassifyCategory(AwinRow row)
    {
        var merchantCategory = NormaliseCategoryPath(row.MerchantCategory);
        var productType = NormaliseCategoryPath(row.ProductType);
        var categoryName = NormaliseCategoryPath(row.CategoryName);

        var categoryText = $"{merchantCategory} {productType} {categoryName}";

        var titleText = Normalise(
            string.Join(
                " ",
                row.ProductName,
                row.BrandName,
                row.ProductModel,
                row.ModelNumber,
                row.Keywords,
                row.Description,
                row.Specifications
            )
        );

        var allText = $"{categoryText} {titleText}";

        if (LooksLikeFullPc(allText))
        {
            return null;
        }

        // Reject rows where the category path (incl. product_type) is clearly non-PC
        if (ContainsAny(categoryText, [
                "audio visual",
                "cameras & camcorders",
                "photo & video",
                "photographic",
                "photo paper",
                "camcorder",
                "filmmaking",
            ]))
        {
            return null;
        }

        // PcNova category-path source of truth.
        if (CategoryContains(merchantCategory, "pc components", "processors"))
        {
            return HasCpuModel(titleText) && !ContainsAny(allText, CpuRejectWords)
                ? Cpu
                : null;
        }

        if (CategoryContains(merchantCategory, "pc components", "graphics cards"))
        {
            return HasGpuModel(titleText) && !ContainsAny(allText, GpuRejectWords)
                ? Gpu
                : null;
        }

        if (CategoryContains(merchantCategory, "pc components", "motherboards"))
        {
            return HasMotherboardSignal(titleText) && !ContainsAny(allText, MotherboardRejectWords)
                ? Motherboard
                : null;
        }

        if (CategoryContains(merchantCategory, "pc components", "memory modules"))
        {
            return HasRamSignal(titleText) && !ContainsAny(allText, RamRejectWords)
                ? Ram
                : null;
        }

        if (CategoryContains(merchantCategory, "storage", "solid state"))
        {
            // External/portable drives share the same category path keywords — skip them
            if (ContainsAny(merchantCategory, ["external", "portable", "personal stock"]))
                return null;

            return HasStorageSignal(titleText) && !ContainsAny(allText, StorageRejectWords)
                ? Storage
                : null;
        }

        // PcNova dumps PSUs into "Other", so category alone is not enough.
        if (CategoryContains(merchantCategory, "pc components", "other"))
        {
            return HasPsuSignal(titleText) && !ContainsAny(allText, PsuRejectWords)
                ? Psu
                : null;
        }

        if (CategoryContains(merchantCategory, "cases & cooling", "cooling & parts"))
        {
            return HasCoolerSignal(titleText) && !ContainsAny(allText, CoolerRejectWords)
                ? Cooler
                : null;
        }

        if (CategoryContains(merchantCategory, "cases & cooling", "cases & parts"))
        {
            // "Case Parts" = fans, filters, accessories — not actual cases
            if (ContainsAny(merchantCategory, ["case parts"]))
                return null;

            return HasCaseSignal(titleText) && !ContainsAny(allText, CaseRejectWords)
                ? Case
                : null;
        }

        // Love Tech Hate Waste (and any retailer using "Computing > X" paths).
        // Title signal used as a secondary filter within each matched category
        // to exclude capture cards, external drives, etc. from the GPU/Storage buckets.
        if (CategoryContains(merchantCategory, "computing", "graphics"))
        {
            return HasGpuModel(titleText) && !ContainsAny(allText, GpuRejectWords)
                ? Gpu
                : null;
        }

        if (CategoryContains(merchantCategory, "computing", "processors"))
        {
            return HasCpuModel(titleText) && !ContainsAny(allText, CpuRejectWords)
                ? Cpu
                : null;
        }

        if (CategoryContains(merchantCategory, "computing", "internal memory"))
        {
            return HasRamSignal(titleText) && !ContainsAny(allText, RamRejectWords)
                ? Ram
                : null;
        }

        if (CategoryContains(merchantCategory, "computing", "data storage"))
        {
            if (ContainsAny(merchantCategory, ["external", "portable", "usb"]))
                return null;

            return HasStorageSignal(titleText) && !ContainsAny(allText, StorageRejectWords)
                ? Storage
                : null;
        }

        return null;
    }

    private static string NormaliseCategoryPath(string value)
    {
        value = value
            .Replace("&gt;", ">")
            .Replace("&amp;", "&")
            .Replace("/", ">")
            .Replace("\\", ">")
            .ToLowerInvariant();

        value = Regex.Replace(value, @"\s+", " ").Trim();

        return value;
    }

    private static bool CategoryContains(string categoryPath, params string[] requiredParts)
    {
        return requiredParts.All(part =>
            categoryPath.Contains(part, StringComparison.OrdinalIgnoreCase)
        );
    }


    private static string ExtractCatalogLabel(string category, AwinRow row)
    {
        var text = Normalise(
            string.Join(
                " ",
                row.ProductName,
                row.BrandName,
                row.ProductModel,
                row.ModelNumber
            )
        );

        return category switch
        {
            Gpu => ExtractGpuModel(text),
            Cpu => ExtractCpuModel(text),
            Motherboard => ExtractMotherboardModel(text),
            Ram => ExtractRamModel(text),
            Storage => ExtractStorageModel(text),
            Psu => ExtractPsuModel(text),
            Cooler => ExtractCoolerModel(text),
            Case => ExtractCaseModel(text),
            _ => ""
        };
    }

    private static string ExtractGpuModel(string text)
    {
        var patterns = new[]
        {
            @"\brtx\s?(5090|5080|5070|5060|4090|4080|4070|4060|3090|3080|3070|3060|3050)\s?(ti|super)?\b",
            @"\bgtx\s?(1660|1650|1080|1070|1060)\s?(ti|super)?\b",
            @"\brx\s?(9070|7900|7800|7700|7600|6950|6900|6800|6700|6600|6500|6400|6300)\s?(xt|gre|xtx)?\b"
        };

        foreach (var pattern in patterns)
        {
            var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);

            if (match.Success)
            {
                return CleanModelLabel(match.Value);
            }
        }

        return "";
    }

    private static string ExtractCpuModel(string text)
    {
        var patterns = new[]
        {
            @"\bryzen\s?[3579]\s?\d{4}x?3?d?\b",
            @"\bcore\s?i[3579][-\s]?\d{4,5}[a-z]{0,3}\b",
            @"\bi[3579][-\s]?\d{4,5}[a-z]{0,3}\b"
        };

        foreach (var pattern in patterns)
        {
            var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);

            if (match.Success)
            {
                return CleanModelLabel(match.Value);
            }
        }

        return "";
    }

    private static string ExtractMotherboardModel(string text)
    {
        var match = Regex.Match(
            text,
            @"\b(a520|a620|b450|b550|b650|b760|b850|x570|x670|x870|z690|z790|z890|h610|h670|h770)\b",
            RegexOptions.IgnoreCase
        );

        return match.Success
            ? CleanModelLabel(match.Value)
            : "";
    }

    private static string ExtractRamModel(string text)
    {
        var ddrMatch = Regex.Match(text, @"\bddr[45]\b", RegexOptions.IgnoreCase);
        var sizeMatch = Regex.Match(text, @"\b(8|16|24|32|48|64|96|128)\s?gb\b", RegexOptions.IgnoreCase);

        if (!ddrMatch.Success || !sizeMatch.Success)
        {
            return "";
        }

        return $"{CleanModelLabel(ddrMatch.Value)} {sizeMatch.Value.ToUpperInvariant().Replace(" ", "")}";
    }

    private static string ExtractStorageModel(string text)
    {
        var capacityMatch = Regex.Match(text, @"\b(\d+)\s?(gb|tb)\b", RegexOptions.IgnoreCase);
        if (!capacityMatch.Success) return "";

        var capacity = capacityMatch.Value.ToUpperInvariant().Replace(" ", "");

        if (ContainsAny(text, ["nvme", "m.2", "pcie", "m2"]))
            return $"{capacity} M.2 NVMe";

        if (ContainsAny(text, ["sata", "2.5\""]))
            return $"{capacity} SATA SSD";

        return $"{capacity} SSD";
    }

    private static string ExtractCoolerModel(string text)
    {
        var aioSize = Regex.Match(text, @"\b(120|240|280|360|420)\s?mm\b", RegexOptions.IgnoreCase);
        if (aioSize.Success)
            return $"AIO {aioSize.Value.ToUpperInvariant().Replace(" ", "")}";

        if (ContainsAny(text, ["liquid", "aio", "all-in-one", "all in one", "hydro", "kraken"]))
            return "AIO Liquid Cooler";

        return "Air Cooler";
    }

    private static string ExtractCaseModel(string text)
    {
        if (ContainsAny(text, ["mini-itx", "mini itx", "itx"]))
            return "Mini-ITX Case";

        if (ContainsAny(text, ["micro-atx", "micro atx", "matx", "m-atx"]))
            return "Micro-ATX Case";

        if (ContainsAny(text, ["full tower", "full-tower", "eatx", "xl-atx"]))
            return "Full-Tower ATX Case";

        return "Mid-Tower ATX Case";
    }

    private static string ExtractPsuModel(string text)
    {
        var wattageMatch = Regex.Match(
            text,
            @"\b(3[0-9]{2}|[4-9][0-9]{2}|1[0-5][0-9]{2})\s?w\b",
            RegexOptions.IgnoreCase
        );

        if (!wattageMatch.Success)
        {
            return "";
        }

        return wattageMatch.Value
            .ToUpperInvariant()
            .Replace(" ", "");
    }

    private static string CanonicalProductLabel(string category, string label)
    {
        var text = label.Trim().ToUpperInvariant();

        if (category == Gpu)
        {
            text = text.Replace("GEFORCE ", "");
            text = text.Replace("RADEON ", "");

            text = Regex.Replace(text, @"\bRTX\s?(\d{4,5})\s?(TI|SUPER)?\b", m =>
            {
                var suffix = m.Groups[2].Success ? $" {ToNiceSuffix(m.Groups[2].Value)}" : "";
                return $"RTX {m.Groups[1].Value}{suffix}";
            });

            text = Regex.Replace(text, @"\bGTX\s?(\d{4,5})\s?(TI|SUPER)?\b", m =>
            {
                var suffix = m.Groups[2].Success ? $" {ToNiceSuffix(m.Groups[2].Value)}" : "";
                return $"GTX {m.Groups[1].Value}{suffix}";
            });

            text = Regex.Replace(text, @"\bRX\s?(\d{4})\s?(XT|XTX|GRE)?\b", m =>
            {
                var suffix = m.Groups[2].Success ? $" {m.Groups[2].Value.ToUpperInvariant()}" : "";
                return $"RX {m.Groups[1].Value}{suffix}";
            });

            text = Regex.Replace(text, @"\bARC\s?A(\d{3})\b", "Arc A$1");
        }

        if (category == Cpu)
        {
            text = text.Replace("INTEL ", "");

            text = Regex.Replace(
                text,
                @"\b(?:CORE\s+)?I([3579])[-\s]?(\d{3,5}\w*)\b",
                "Core i$1-$2"
            );

            text = Regex.Replace(
                text,
                @"\bRYZEN\s?(\d)\s+(\w+)\b",
                "Ryzen $1 $2"
            );
        }

        text = Regex.Replace(text, @"\s+", " ").Trim();

        return text;
    }

    private static string ToNiceSuffix(string value)
    {
        return value.ToUpperInvariant() switch
        {
            "TI" => "Ti",
            "SUPER" => "Super",
            _ => value
        };
    }

    private static async Task<ProductCatalog> GetOrCreateCatalogProductAsync(
        DealFinderDbContext dbContext,
        Dictionary<(string Category, string Label), ProductCatalog> catalogCache,
        string category,
        string label,
        string description,
        CancellationToken ct)
    {
        if (catalogCache.TryGetValue((category, label), out var existing))
        {
            return existing;
        }

        var catalogProduct = new ProductCatalog
        {
            Category = category,
            Label = label,
            SearchQuery = label,
            Description = description,
            IsActive = true
        };

        await dbContext.ProductCatalog.AddAsync(catalogProduct, ct);
        // Save immediately — the listing row needs this product's generated Id.
        await dbContext.SaveChangesAsync(ct);

        catalogCache[(category, label)] = catalogProduct;

        return catalogProduct;
    }

    private static async Task UpsertListingAsync(
        DealFinderDbContext dbContext,
        Dictionary<string, ProductListing> listingCache,
        ProductListing listing,
        CancellationToken ct)
    {
        if (!listingCache.TryGetValue(listing.Url, out var existing))
        {
            await dbContext.ProductListings.AddAsync(listing, ct);
            listingCache[listing.Url] = listing;
            return;
        }

        existing.Title = listing.Title;
        existing.Description = listing.Description;
        existing.Price = listing.Price;
        existing.ShippingPrice = listing.ShippingPrice;
        existing.Condition = listing.Condition;
        existing.Marketplace = listing.Marketplace;
        existing.ImageUrl = listing.ImageUrl;
        existing.Category = listing.Category;
        existing.ProductCatalogId = listing.ProductCatalogId;
        existing.LastSeenUtc = DateTime.UtcNow;
    }

    private static bool IsAvailable(AwinRow row)
    {
        if (!IsTruthyOrEmpty(row.InStock))
        {
            return false;
        }

        if (!IsTruthyOrEmpty(row.IsForSale))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(row.StockStatus) &&
            !row.StockStatus.Equals("in stock", StringComparison.OrdinalIgnoreCase) &&
            !row.StockStatus.Equals("available", StringComparison.OrdinalIgnoreCase) &&
            !row.StockStatus.Equals("instock", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(row.StockQuantity) &&
            decimal.TryParse(row.StockQuantity, NumberStyles.Any, CultureInfo.InvariantCulture, out var qty) &&
            qty <= 0)
        {
            return false;
        }

        return true;
    }

    private static bool IsTruthyOrEmpty(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        return value.Equals("1", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("true", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("yes", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("y", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("in stock", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("available", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("for sale", StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryParseDecimal(string value, out decimal result)
    {
        value = value
            .Replace("£", "")
            .Replace(",", "")
            .Trim();

        return decimal.TryParse(
            value,
            NumberStyles.Any,
            CultureInfo.InvariantCulture,
            out result
        );
    }

    private static string NormaliseCondition(AwinRow row)
    {
        if (!string.IsNullOrWhiteSpace(row.Condition))
        {
            return row.Condition.ToLowerInvariant();
        }

        var text = Normalise($"{row.ProductName} {row.MerchantCategory} {row.CategoryPath}");

        if (ContainsAny(text, ["used", "refurb", "open box", "open-box", "b grade", "b-grade", "ex display"]))
        {
            return "refurbished/open-box";
        }

        return "new";
    }

    private static string BuildCatalogDescription(string category, string label)
    {
        return category switch
        {
            Gpu => $"{label} graphics card listings from retailer feeds.",
            Cpu => $"{label} processor listings from retailer feeds.",
            Motherboard => $"{label} motherboard listings from retailer feeds.",
            Ram => $"{label} memory listings from retailer feeds.",
            Storage => $"{label} SSD listings from retailer feeds.",
            Psu => $"{label} power supply listings from retailer feeds.",
            Cooler => $"{label} CPU cooler listings from retailer feeds.",
            Case => $"{label} PC case listings from retailer feeds.",
            _ => $"{label} listings from retailer feeds."
        };
    }

    private static string FirstNonEmpty(params string[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? "";
    }

    private static bool ContainsAny(string text, IEnumerable<string> words)
    {
        return words.Any(word => text.Contains(word, StringComparison.OrdinalIgnoreCase));
    }

    private static string Normalise(string value)
    {
        return Regex.Replace(value.ToLowerInvariant(), @"\s+", " ").Trim();
    }

    private static string CleanModelLabel(string value)
    {
        value = Regex.Replace(value, @"\s+", " ").Trim();

        return value.ToUpperInvariant()
            .Replace("RYZEN", "Ryzen")
            .Replace("CORE", "Core")
            .Replace("RTX", "RTX")
            .Replace("GTX", "GTX")
            .Replace("RX", "RX")
            .Replace("DDR", "DDR");
    }

    private static bool HasGpuModel(string text)
    {
        return Regex.IsMatch(
            text,
            @"\b(geforce\s+)?rtx\s?[2345][0-9]{3}\s?(ti|super)?\b|\b(geforce\s+)?gtx\s?(10|16)[0-9]{2}\s?(ti|super)?\b|\b(radeon\s+)?rx\s?[5679][0-9]{3}\s?(xt|gre|xtx)?\b",
            RegexOptions.IgnoreCase
        );
    }

    private static bool HasCpuModel(string text)
    {
        return Regex.IsMatch(
            text,
            @"\b(ryzen\s?[3579]\s?\d{4}|core\s?i[3579]|i[3579][-\s]?\d{4,5})\b",
            RegexOptions.IgnoreCase
        );
    }

    private static bool HasMotherboardSignal(string text)
    {
        return Regex.IsMatch(
            text,
            @"\b(a520|a620|b450|b550|b650|b760|b850|x570|x670|x870|z690|z790|z890|h610|h670|h770|motherboard|mainboard)\b",
            RegexOptions.IgnoreCase
        );
    }

    private static bool HasRamSignal(string text)
    {
        return Regex.IsMatch(
            text,
            @"\b(ddr4|ddr5)\b",
            RegexOptions.IgnoreCase
        );
    }

    private static bool HasCoolerSignal(string text)
    {
        return Regex.IsMatch(
            text,
            @"\b(cpu\s?cooler|heatsink|heat\s?sink|air\s?cooler|liquid\s?cooler|water\s?cooler|tower\s?cooler|aio|all[-\s]in[-\s]one)\b",
            RegexOptions.IgnoreCase
        );
    }

    private static bool HasCaseSignal(string text)
    {
        return Regex.IsMatch(
            text,
            @"\b(pc\s?case|computer\s?case|gaming\s?case|tower\s?case|chassis|mid[-\s]tower|full[-\s]tower|mini[-\s]tower|atx\s?case|itx\s?case)\b",
            RegexOptions.IgnoreCase
        );
    }

    private static bool HasStorageSignal(string text)
    {
        return Regex.IsMatch(
            text,
            @"\b(ssd|nvme|m\.2|solid\s?state|nand)\b",
            RegexOptions.IgnoreCase
        );
    }

    private static bool HasPsuSignal(string text)
    {
        var hasWattage = Regex.IsMatch(
            text,
            @"\b(3[0-9]{2}|[4-9][0-9]{2}|1[0-5][0-9]{2})\s?w\b",
            RegexOptions.IgnoreCase
        );

        var hasPsuSignal = Regex.IsMatch(
            text,
            @"\b(psu|power supply|power-supply|atx|sfx|80\s?plus|80\+|modular)\b",
            RegexOptions.IgnoreCase
        );

        return hasWattage && hasPsuSignal;
    }

    private static readonly string[] GlobalRejectWords =
    [
        "laptop",
        "notebook",
        "desktop pc",
        "workstation",
        "server",
        "printer",
        "scanner",
        "battery",
        "charger",
        "adapter",
        "adaptor",
        "cable",
        "holder",
        "mount",
        "stand",
        "riser",
        "license",
        "subscription",
        "windows 365",
        "network switch",
        "ethernet switch",
        "access point",
        "gaming pc",
    "desktop pc",
    "desktop computer",
    "gaming desktop",
    "prebuilt",
    "pre-built",
    "system",
    "workstation",
    "medion erazer",
    "erazer hunter",
    ];

    private static bool LooksLikeFullPc(string text)
    {
        return ContainsAny(text, [
            "gaming pc",
            "desktop pc",
            "desktop computer",
            "gaming desktop",
            "prebuilt",
            "pre-built",
            "medion erazer",
            "erazer hunter",
            "pc system"
        ]);
    }

    private static readonly string[] GpuRejectWords =
    [
        "quadro",
        "firepro",
        "radeon pro",
        "rtx pro",
        "rtx a",
        "rtx ada",
        "t1000",
        "t600",
        "t400",
        "matrox",
        "tesla",
        "grid",
        "nvs",
        "waterblock",
        "backplate",
        "graphics card holder",
        "graphics card support"
    ];

    private static readonly string[] CpuRejectWords =
    [
        "xeon",
        "epyc",
        "opteron",
        "cooler",
        "fan",
        "heatsink"
    ];

    private static readonly string[] MotherboardRejectWords =
    [
        "server",
        "workstation",
        "sp3",
        "sp6",
        "lga 4094",
        "romed",
        "sienad",
        "daughterboard",
        "controller card",
        "network card",
        "interface card"
    ];

    private static readonly string[] RamRejectWords =
    [
        "ddr2",
        "ddr3",
        "ecc",
        "registered",
        "rdimm",
        "lrdimm",
        "sodimm",
        "so-dimm",
        "server",
        "laptop",
        "replacement",
        "replaces",
        "memory card",
        "microsd",
        "cfast",
        "printer"
    ];

    private static readonly string[] CoolerRejectWords =
    [
        "laptop",
        "notebook",
        "gpu cooler",
        "graphics card cooler",
        "vga cooler",
        "thermal paste",
        "thermal compound",
        "thermal pad",
        "fan only",
        "replacement fan",
        "case fan",
        "chassis fan",
    ];

    private static readonly string[] CaseRejectWords =
    [
        "laptop",
        "notebook",
        "phone case",
        "tablet case",
        "carrying case",
        "hard case",
        "travel case",
        "storage case",
        "protective case",
        "drive caddy",
        "hdd caddy",
    ];

    private static readonly string[] StorageRejectWords =
    [
        "server",
        "nas",
        "enterprise",
        "optane",
        "flash drive",
        "usb drive",
        "usb stick",
        "pen drive",
        "memory card",
        "microsd",
        "sd card",
        "external hdd",
        "external ssd",
        "external solid state",
        "portable ssd",
        "portable solid state",
        "portable hard",
        "portable drive",
        "personal stock",
    ];

    private static readonly string[] PsuRejectWords =
    [
        "ups",
        "battery",
        "charger",
        "adapter",
        "server",
        "dell",
        "hp ",
        "hpe ",
        "lenovo",
        "fujitsu",
        "redundant",
        "hot plug",
        "hot-plug",
        "poe",
        "cisco",
        "netgear"
    ];

    private sealed class AwinRow
    {
        public string ProductName { get; set; } = "";
        public string Description { get; set; } = "";
        public string ShortDescription { get; set; } = "";
        public string Specifications { get; set; } = "";

        public string SearchPrice { get; set; } = "";
        public string DeliveryCost { get; set; } = "";
        public string Currency { get; set; } = "";

        public string MerchantName { get; set; } = "";
        public string MerchantCategory { get; set; } = "";
        public string CategoryName { get; set; } = "";
        public string CategoryPath { get; set; } = "";
        public string SecondCategory { get; set; } = "";
        public string ThirdCategory { get; set; } = "";
        public string ProductType { get; set; } = "";

        public string BrandName { get; set; } = "";
        public string ProductModel { get; set; } = "";
        public string ModelNumber { get; set; } = "";
        public string Keywords { get; set; } = "";

        public string Condition { get; set; } = "";
        public string InStock { get; set; } = "";
        public string StockStatus { get; set; } = "";
        public string StockQuantity { get; set; } = "";
        public string IsForSale { get; set; } = "";

        public string AwinDeepLink { get; set; } = "";
        public string MerchantDeepLink { get; set; } = "";

        public string MerchantImageUrl { get; set; } = "";
        public string LargeImage { get; set; } = "";
        public string AwinImageUrl { get; set; } = "";
        public string MerchantThumbUrl { get; set; } = "";
    }
}