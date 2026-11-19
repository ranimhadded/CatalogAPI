using Microsoft.EntityFrameworkCore;
using ProductCatalog.Api.Models;

namespace ProductCatalog.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Product>(entity =>
        {
            entity.Property(p => p.Name)
                .IsRequired()
                .HasMaxLength(30);

            entity.Property(p => p.Price)
                .HasPrecision(10, 2);
        });
    }
}