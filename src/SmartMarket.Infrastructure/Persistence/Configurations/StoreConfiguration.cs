using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartMarket.Domain.Entities;

namespace SmartMarket.Infrastructure.Persistence.Configurations;

public class StoreConfiguration : IEntityTypeConfiguration<Store>
{
    public void Configure(EntityTypeBuilder<Store> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Name).HasMaxLength(150).IsRequired();
        builder.Property(s => s.Description).HasMaxLength(500);

        builder.HasMany(s => s.Products)
               .WithOne(p => p.Store)
               .HasForeignKey(p => p.StoreId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}