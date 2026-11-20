using CampusX.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CampusX.Data.DbContexts;

// Keep the existing UserRole enum as the single role source; no parallel role membership store.
public class CampusXDbContext : IdentityUserContext<ApplicationUser, Guid>
{
    public CampusXDbContext(DbContextOptions<CampusXDbContext> options) : base(options) { }

    public DbSet<ApplicationUser> ApplicationUsers => Set<ApplicationUser>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Report> Reports => Set<Report>();
    public DbSet<ContactInquiry> ContactInquiries => Set<ContactInquiry>();
    public DbSet<PromotionCreditPurchase> PromotionCreditPurchases => Set<PromotionCreditPurchase>();
    public DbSet<ProductPromotion> ProductPromotions => Set<ProductPromotion>();
    public DbSet<UniversityEmailVerification> UniversityEmailVerifications => Set<UniversityEmailVerification>();
    public DbSet<UniversityVerificationAttempt> UniversityVerificationAttempts => Set<UniversityVerificationAttempt>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CampusXDbContext).Assembly);
    }
}
