using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CampusX.Domain.Enums;

namespace CampusX.Domain.Entities;

public class Product
{
    public Guid Id { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public decimal Price { get; private set; }
    public string ImageUrl { get; private set; } = string.Empty;
    public ProductStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsDeleted { get; private set; }

    // Foreign Keys
    public Guid SellerId { get; private set; }
    public Guid CategoryId { get; private set; }

    // Navigation Properties
    public ApplicationUser Seller { get; private set; } = null!;
    public Category Category { get; private set; } = null!;
    public ICollection<Report> Reports { get; private set; } = new List<Report>();
    public ICollection<ContactInquiry> Inquiries { get; private set; } = new List<ContactInquiry>();

    private Product() { }

    public Product(string title, string description, decimal price, string imageUrl, Guid sellerId, Guid categoryId)
    {
        if (price <= 0)
            throw new ArgumentException("Price must be greater than zero.", nameof(price));

        Id = Guid.NewGuid();
        Title = title;
        Description = description;
        Price = price;
        ImageUrl = imageUrl;
        SellerId = sellerId;
        CategoryId = categoryId;
        Status = ProductStatus.Available;
        CreatedAt = DateTime.UtcNow;
        IsDeleted = false;
    }

    // Domain Behaviors
    public void UpdateDetails(string title, string description, decimal price, string imageUrl, Guid categoryId)
    {
        if (price <= 0)
            throw new ArgumentException("Price must be greater than zero.", nameof(price));

        Title = title;
        Description = description;
        Price = price;
        ImageUrl = imageUrl;
        CategoryId = categoryId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ChangeStatus(ProductStatus newStatus)
    {
        Status = newStatus;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsSold() => ChangeStatus(ProductStatus.Sold);
    public void SoftDelete() => IsDeleted = true;
}
