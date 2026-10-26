using CampusX.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace CampusX.Models
{
    public class Product
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public ProductStatus Status { get; set; } = ProductStatus.Available;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; } = false;

        // Foreign Keys
        public Guid SellerId { get; set; }
        public Guid CategoryId { get; set; }

        // Navigation Properties
        public ApplicationUser Seller { get; set; } = null!;
        public Category Category { get; set; } = null!;
        public ICollection<Report> Reports { get; set; } = new List<Report>();
        public ICollection<ContactInquiry> Inquiries { get; set; } = new List<ContactInquiry>();

        public void UpdateDetails(string name, string description, decimal price, string imageUrl, Guid categoryId)
        {
            Name = name;
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
    }
}