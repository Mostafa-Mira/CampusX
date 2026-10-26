using Microsoft.EntityFrameworkCore;
using CampusX.Entities;
using CampusX.Enums;

namespace CampusX
{
    public class CampusXDbContext : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer("Server=DESKTOP-46KJRL8\\SQLEXPRESS;Database=CampusXDb;Trusted_Connection=True;TrustServerCertificate=True");
        }

        public DbSet<ApplicationUser> ApplicationUsers { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Report> Reports { get; set; }
        public DbSet<ContactInquiry> ContactInquiries { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 1. ربط ContactInquiries (Buyer & Seller)
            modelBuilder.Entity<ContactInquiry>()
                .HasOne(c => c.Buyer)
                .WithMany(u => u.BuyerInquiries)
                .HasForeignKey(c => c.BuyerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ContactInquiry>()
                .HasOne(c => c.Seller)
                .WithMany(u => u.SellerInquiries)
                .HasForeignKey(c => c.SellerId)
                .OnDelete(DeleteBehavior.Restrict);

            // 2. ربط Reports (Reporter & TargetUser)
            modelBuilder.Entity<Report>()
                .HasOne(r => r.Reporter)
                .WithMany(u => u.ReportsSubmitted)
                .HasForeignKey(r => r.ReporterId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Report>()
                .HasOne(r => r.TargetUser)
                .WithMany()
                .HasForeignKey(r => r.TargetUserId)
                .OnDelete(DeleteBehavior.Restrict);
            // 3. ضبط دقة السعر للـ Product
            modelBuilder.Entity<Product>()
                .Property(p => p.Price)
                .HasPrecision(18, 2);
        }
    }
}