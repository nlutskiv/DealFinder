using DealFinder.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DealFinder.Api.Data;

public class DealFinderDbContext : DbContext
{
    public DealFinderDbContext(DbContextOptions<DealFinderDbContext> options)
        : base(options)
    {
    }

    public DbSet<AiEvaluationCache> AiEvaluationCaches => Set<AiEvaluationCache>();
    public DbSet<ProductCatalog> ProductCatalog => Set<ProductCatalog>();
    public DbSet<ProductListing> ProductListings => Set<ProductListing>();
    public DbSet<PriceSnapshot> PriceSnapshots => Set<PriceSnapshot>();
    public DbSet<SavedBuild> SavedBuilds => Set<SavedBuild>();
    public DbSet<PriceAlert> PriceAlerts => Set<PriceAlert>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AiEvaluationCache>()
            .HasIndex(x => x.UrlHash)
            .IsUnique();

        modelBuilder.Entity<AiEvaluationCache>()
            .Property(x => x.Url)
            .IsRequired();

        modelBuilder.Entity<AiEvaluationCache>()
            .Property(x => x.UrlHash)
            .IsRequired();

        modelBuilder.Entity<ProductListing>()
            .HasIndex(x => x.Url)
            .IsUnique();

        modelBuilder.Entity<ProductListing>()
            .Property(x => x.Title)
            .IsRequired();

        modelBuilder.Entity<ProductListing>()
            .Property(x => x.Url)
            .IsRequired();

        modelBuilder.Entity<ProductListing>()
            .Property(x => x.Category)
            .IsRequired();
        modelBuilder.Entity<ProductCatalog>()
            .HasIndex(x => x.SearchQuery)
            .IsUnique();

        modelBuilder.Entity<ProductListing>()
            .HasIndex(x => x.ProductCatalogId);

        modelBuilder.Entity<PriceSnapshot>()
            .HasIndex(x => new { x.CatalogProductId, x.RecordedDate });

        modelBuilder.Entity<PriceAlert>()
            .HasIndex(x => new { x.CatalogProductId, x.IsActive });
    }
}