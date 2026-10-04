using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartMarket.Domain.Entities;

namespace SmartMarket.Infrastructure.Persistence.Configurations;

public class MerchantSubscriptionConfiguration : IEntityTypeConfiguration<MerchantSubscription>
{
    public void Configure(EntityTypeBuilder<MerchantSubscription> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.PlanType)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(s => s.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(s => s.SubscripedAt).IsRequired();
        builder.Property(s => s.ExpiresAt).IsRequired();
        builder.HasIndex(s => s.ExpiresAt);

        builder.Ignore(s => s.IsActive);

        builder.HasOne(s => s.Store)
            .WithMany()
            .HasForeignKey(s => s.StoreId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}