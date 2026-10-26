using CampusX.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CampusX.Data.Configurations;

public class ContactInquiryConfiguration : IEntityTypeConfiguration<ContactInquiry>
{
    public void Configure(EntityTypeBuilder<ContactInquiry> builder)
    {
        builder.ToTable("ContactInquiries");
        builder.HasKey(inquiry => inquiry.Id);
        builder.Property(inquiry => inquiry.Message).IsRequired();

        builder.HasOne(inquiry => inquiry.Buyer)
            .WithMany(user => user.BuyerInquiries)
            .HasForeignKey(inquiry => inquiry.BuyerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(inquiry => inquiry.Product)
            .WithMany(product => product.Inquiries)
            .HasForeignKey(inquiry => inquiry.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
