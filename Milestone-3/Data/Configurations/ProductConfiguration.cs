using CampusX.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CampusX.Data.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products", table =>
            table.HasCheckConstraint("CK_Products_Price_NonNegative", "[Price] >= 0"));
        builder.HasKey(product => product.Id);
        builder.Property(product => product.Name).IsRequired();
        builder.Property(product => product.Description).IsRequired();
        builder.Property(product => product.ImageUrl).IsRequired();
        builder.Property(product => product.Price).HasPrecision(18, 2);
        // Check that ownership and moderation state still match when a loaded listing is saved.
        builder.Property(product => product.SellerId).IsConcurrencyToken();
        builder.Property(product => product.IsDeleted).IsConcurrencyToken();

        builder.HasOne(product => product.Seller)
            .WithMany(user => user.Products)
            .HasForeignKey(product => product.SellerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(product => product.Category)
            .WithMany(category => category.Products)
            .HasForeignKey(product => product.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
