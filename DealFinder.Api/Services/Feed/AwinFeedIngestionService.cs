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
                listing.Category,
                catalogLabel,
                catalogDescription,
                ct
            );

            listing.ProductCatalogId = catalogProduct.Id;

            await UpsertListingAsync(dbContext, listing, ct);
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
        var categoryText = Normalise(
            string.Join(
                " ",
                row.MerchantCategory,
                row.CategoryName,
                row.CategoryPath,
                row.SecondCategory,
                row.ThirdCategory,
                row.ProductType
            )
        );

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

        if (ContainsAny(allText, GlobalRejectWords))
        {
            return null;
        }

        // Use title/model patterns first because PcNova categories may be broad/generic.
        if (HasGpuModel(titleText) && !ContainsAny(allText, GpuRejectWords))
        {
            return Gpu;
        }

        if (HasCpuModel(titleText) && !ContainsAny(allText, CpuRejectWords))
        {
            return Cpu;
        }

        if (HasMotherboardSignal(allText) && !ContainsAny(allText, MotherboardRejectWords))
        {
            return Motherboard;
        }

        if (HasRamSignal(allText) && !ContainsAny(allText, RamRejectWords))
        {
            return Ram;
        }

        if (HasPsuSignal(allText) && !ContainsAny(allText, PsuRejectWords))
        {
            return Psu;
        }

        return null;
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
            Psu => ExtractPsuModel(text),
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

    private async Task<ProductCatalog> GetOrCreateCatalogProductAsync(
        DealFinderDbContext dbContext,
        string category,
        string label,
        string description,
        CancellationToken ct)
    {
        var existing = await dbContext.ProductCatalog
            .FirstOrDefaultAsync(
                product =>
                    product.Category == category &&
                    product.Label == label,
                ct
            );

        if (existing is not null)
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
        await dbContext.SaveChangesAsync(ct);

        return catalogProduct;
    }

    private static async Task UpsertListingAsync(
        DealFinderDbContext dbContext,
        ProductListing listing,
        CancellationToken ct)
    {
        var existing = await dbContext.ProductListings
            .FirstOrDefaultAsync(
                product => product.Url == listing.Url,
                ct
            );

        if (existing is null)
        {
            await dbContext.ProductListings.AddAsync(listing, ct);
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
            Psu => $"{label} power supply listings from retailer feeds.",
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

    private static bool LooksLikeGpuCategory(string text)
    {
        return ContainsAny(text, ["graphics card", "graphics cards", "gpu"]);
    }

    private static bool LooksLikeCpuCategory(string text)
    {
        return ContainsAny(text, ["processor", "processors", "cpu"]);
    }

    private static bool LooksLikeMotherboardCategory(string text)
    {
        return ContainsAny(text, ["motherboard", "motherboards", "mainboard"]);
    }

    private static bool LooksLikeRamCategory(string text)
    {
        return ContainsAny(text, ["memory", "memory module", "memory modules", "ram"]);
    }

    private static bool LooksLikePsuCategory(string text)
    {
        return ContainsAny(text, ["power supply", "power supplies", "psu"]);
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

    private static bool HasPsuSignal(string text)
    {
        var hasWattage = Regex.IsMatch(
            text,
            @"\b(3[0-9]{2}|[4-9][0-9]{2}|1[0-5][0-9]{2})\s?w\b",
            RegexOptions.IgnoreCase
        );

        var hasPsuSignal = Regex.IsMatch(
            text,
            @"\b(atx|sfx|80\s?plus|80\+|modular|psu|power supply)\b",
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