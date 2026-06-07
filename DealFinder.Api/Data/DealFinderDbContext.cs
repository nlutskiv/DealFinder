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
    public DbSet<ProductListing> ProductListings => Set<ProductListing>();

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
    }
}