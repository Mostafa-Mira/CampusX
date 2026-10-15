using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CampusX.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CampusX.Infrastructure.Persistence;

public class CampusXDbContext : DbContext
{
    public CampusXDbContext(DbContextOptions<CampusXDbContext> options) : base(options)
    {
    }

    public DbSet<ApplicationUser> Users => Set<ApplicationUser>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Report> Reports => Set<Report>();
    public DbSet<ContactInquiry> ContactInquiries => Set<ContactInquiry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Category Seed Data
        modelBuilder.Entity<Category>().HasData(
            new Category("Books", "Academic and study books", "bi-book"),
            new Category("Electronics", "Gadgets and laptops", "bi-laptop"),
            new Category("Tools", "Engineering & lab tools", "bi-tools")
        );
    }
}
