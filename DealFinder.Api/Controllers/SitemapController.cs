using System.Text;
using DealFinder.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace DealFinder.Api.Controllers;

[ApiController]
public class SitemapController : ControllerBase
{
    private const string BaseUrl = "https://www.dealfinder.tech";

    private static readonly (string Path, string Priority)[] StaticPages =
    [
        ("/", "1.0"),
        ("/marketplace", "0.9"),
        ("/pc-assembler", "0.9"),
        ("/rankings", "0.8"),
        ("/about", "0.7"),
        ("/contact", "0.6"),
        ("/privacy", "0.5"),
        ("/terms", "0.5"),
    ];

    private static readonly string[] Categories =
        ["gpu", "cpu", "motherboard", "ram", "storage", "psu", "case", "cooler"];

    private readonly IDbContextFactory<DealFinderDbContext> _dbFactory;
    private readonly IMemoryCache _cache;

    public SitemapController(IDbContextFactory<DealFinderDbContext> dbFactory, IMemoryCache cache)
    {
        _dbFactory = dbFactory;
        _cache = cache;
    }

    [HttpGet("api/sitemap.xml")]
    public async Task<IActionResult> Get()
    {
        var xml = await _cache.GetOrCreateAsync("sitemap-xml", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(6);
            return await BuildXmlAsync();
        });

        return Content(xml!, "application/xml", Encoding.UTF8);
    }

    private async Task<string> BuildXmlAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var products = await db.ProductCatalog
            .Where(c => c.IsActive)
            .Where(c => db.ProductListings.Any(l => l.ProductCatalogId == c.Id && l.Price > 0))
            .Select(c => new { c.Id, c.Label })
            .ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine("""<?xml version="1.0" encoding="UTF-8"?>""");
        sb.AppendLine("""<urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">""");

        foreach (var (path, priority) in StaticPages)
        {
            AppendUrl(sb, path, priority);
        }

        foreach (var category in Categories)
        {
            AppendUrl(sb, $"/rankings/{category}", "0.8");
        }

        foreach (var product in products)
        {
            AppendUrl(sb, $"/product/{product.Id}/{Slugify(product.Label)}", "0.7");
        }

        sb.AppendLine("</urlset>");
        return sb.ToString();
    }

    private static void AppendUrl(StringBuilder sb, string path, string priority)
    {
        sb.AppendLine("  <url>");
        sb.AppendLine($"    <loc>{BaseUrl}{path}</loc>");
        sb.AppendLine($"    <priority>{priority}</priority>");
        sb.AppendLine("  </url>");
    }

    public static string Slugify(string label)
    {
        var sb = new StringBuilder(label.Length);
        var lastDash = true;

        foreach (var ch in label.ToLowerInvariant())
        {
            if (char.IsAsciiLetterOrDigit(ch))
            {
                sb.Append(ch);
                lastDash = false;
            }
            else if (!lastDash)
            {
                sb.Append('-');
                lastDash = true;
            }
        }

        return sb.ToString().TrimEnd('-');
    }
}
