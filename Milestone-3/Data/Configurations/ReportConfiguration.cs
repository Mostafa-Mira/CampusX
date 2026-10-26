using CampusX.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CampusX.Data.Configurations;

public class ReportConfiguration : IEntityTypeConfiguration<Report>
{
    public void Configure(EntityTypeBuilder<Report> builder)
    {
        builder.ToTable("Reports", table =>
            table.HasCheckConstraint("CK_Reports_ExactlyOneTarget",
                "([TargetProductId] IS NOT NULL AND [TargetUserId] IS NULL) OR " +
                "([TargetProductId] IS NULL AND [TargetUserId] IS NOT NULL)"));
        builder.HasKey(report => report.Id);
        builder.Property(report => report.Reason).IsRequired();

        builder.HasOne(report => report.Reporter)
            .WithMany(user => user.ReportsSubmitted)
            .HasForeignKey(report => report.ReporterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(report => report.TargetProduct)
            .WithMany(product => product.Reports)
            .HasForeignKey(report => report.TargetProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(report => report.TargetUser)
            .WithMany()
            .HasForeignKey(report => report.TargetUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
