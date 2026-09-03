using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartMarket.Domain.Entities;

namespace SmartMarket.Infrastructure.Persistence.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
               .HasMaxLength(200)
               .IsRequired();

        builder.Property(p => p.Description)
               .HasMaxLength(1000);

        builder.Property(p => p.Price)
               .HasPrecision(18, 2)
               .IsRequired();

        builder.Property(p => p.StockQuantity)
               .IsRequired();

        builder.HasOne(p => p.Store)
               .WithMany(s => s.Products)
               .HasForeignKey(p => p.StoreId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.Category)
               .WithMany(c => c.Products)
               .HasForeignKey(p => p.CategoryId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}