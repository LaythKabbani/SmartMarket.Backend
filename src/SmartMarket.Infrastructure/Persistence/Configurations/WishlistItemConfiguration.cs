using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartMarket.Domain.Entities;

namespace SmartMarket.Infrastructure.Persistence.Configurations;

public class WishlistItemConfiguration : IEntityTypeConfiguration<WishlistItem>
{
    public void Configure(EntityTypeBuilder<WishlistItem> builder)
    {
        builder.HasKey(w => w.Id);

        builder.HasOne(w => w.User)
               .WithMany()
               .HasForeignKey(c => c.UserId)
               .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(w => w.Product)
               .WithMany()
               .HasForeignKey(w => w.ProductId)
               .OnDelete(DeleteBehavior.NoAction);
    }
}