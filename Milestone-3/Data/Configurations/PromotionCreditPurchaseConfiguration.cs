using CampusX.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CampusX.Data.Configurations;

public class PromotionCreditPurchaseConfiguration : IEntityTypeConfiguration<PromotionCreditPurchase>
{
    public void Configure(EntityTypeBuilder<PromotionCreditPurchase> builder)
    {
        builder.ToTable("PromotionCreditPurchases", table =>
            table.HasCheckConstraint("CK_PromotionCreditPurchases_Credits_Positive", "[CreditsPurchased] > 0"));
        builder.HasKey(purchase => purchase.Id);
        builder.Property(purchase => purchase.Amount).HasPrecision(18, 2);
        builder.Property(purchase => purchase.ProviderReference).IsRequired().HasMaxLength(128);
        builder.HasIndex(purchase => purchase.BuyerId);
        builder.HasOne(purchase => purchase.Buyer).WithMany(user => user.PromotionCreditPurchases)
            .HasForeignKey(purchase => purchase.BuyerId).OnDelete(DeleteBehavior.Restrict);
    }
}
