using CampusX.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CampusX.Data.Configurations;

public class UniversityEmailVerificationConfiguration : IEntityTypeConfiguration<UniversityEmailVerification>
{
    public void Configure(EntityTypeBuilder<UniversityEmailVerification> builder)
    {
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Recipient).HasMaxLength(256).IsRequired();
        builder.Property(item => item.CodeHash).HasMaxLength(1024).IsRequired();
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(item => item.UserId);
        builder.HasIndex(item => item.UserId).IsUnique().HasFilter("[IsActive] = 1");
        builder.HasIndex(item => new { item.UserId, item.CreatedAt });
        builder.HasIndex(item => new { item.Recipient, item.CreatedAt });
    }
}

public class UniversityVerificationAttemptConfiguration : IEntityTypeConfiguration<UniversityVerificationAttempt>
{
    public void Configure(EntityTypeBuilder<UniversityVerificationAttempt> builder)
    {
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Recipient).HasMaxLength(256).IsRequired();
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(item => item.UserId);
        builder.HasIndex(item => new { item.UserId, item.AttemptedAt });
        builder.HasIndex(item => new { item.Recipient, item.AttemptedAt });
    }
}
