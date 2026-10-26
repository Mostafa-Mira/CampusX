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
        builder.Property(user => user.PasswordHash).IsRequired();
    }
}
