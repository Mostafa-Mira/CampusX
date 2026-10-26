using CampusX.Models;
using Microsoft.EntityFrameworkCore;

namespace CampusX.Data.DbContexts;

public class CampusXDbContext : DbContext
{
    public CampusXDbContext(DbContextOptions<CampusXDbContext> options) : base(options) { }

    public DbSet<ApplicationUser> ApplicationUsers => Set<ApplicationUser>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Report> Reports => Set<Report>();
    public DbSet<ContactInquiry> ContactInquiries => Set<ContactInquiry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CampusXDbContext).Assembly);
    }
}
