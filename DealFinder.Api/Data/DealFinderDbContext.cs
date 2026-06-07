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
    }
}