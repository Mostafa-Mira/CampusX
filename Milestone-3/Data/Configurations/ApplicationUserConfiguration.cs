using CampusX.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CampusX.Data.Configurations;

public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.ToTable("ApplicationUsers");
        builder.HasKey(user => user.Id);
        builder.Property(user => user.Email).IsRequired().HasMaxLength(256);
        builder.HasIndex(user => user.Email).IsUnique();
        builder.HasIndex(user => user.NormalizedEmail).HasDatabaseName("EmailIndex")
            .IsUnique().HasFilter("[NormalizedEmail] IS NOT NULL");
        // External-login users may eventually have no local password; existing hashes are preserved.
        builder.Property(user => user.PasswordHash).IsRequired(false);
        builder.Property(user => user.UniversityEmail).HasMaxLength(256);
        builder.Property(user => user.UniversityEmailConfirmed).HasDefaultValue(false);
        builder.Property(user => user.VerifiedUniversityEmail).HasMaxLength(256);
        builder.HasIndex(user => user.VerifiedUniversityEmail).IsUnique()
            .HasFilter("[VerifiedUniversityEmail] IS NOT NULL");
    }
}
