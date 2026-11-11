using CampusX.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CampusX.Data.Configurations;

public class ProductPromotionConfiguration : IEntityTypeConfiguration<ProductPromotion>
{
    public void Configure(EntityTypeBuilder<ProductPromotion> builder)
    {
        builder.ToTable("ProductPromotions", table =>
            table.HasCheckConstraint("CK_ProductPromotions_Expiry", "[ExpiresAt] > [ActivatedAt]"));
        builder.HasKey(promotion => promotion.Id);
        builder.HasIndex(promotion => new { promotion.ProductId, promotion.ExpiresAt });
        builder.HasOne(promotion => promotion.Product).WithMany(product => product.Promotions)
            .HasForeignKey(promotion => promotion.ProductId).OnDelete(DeleteBehavior.Restrict);
    }
}
