using CampusX.Enums;

namespace CampusX.ViewModels;

public class ProductDetailsViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string? ImageUrl { get; set; }
    public string CategoryName { get; set; } = "Uncategorized";
    public ProductStatus Status { get; set; }
    public string SellerName { get; set; } = "Seller information unavailable";
    public string? SellerCollegeName { get; set; }
    public string? SellerEmail { get; set; }
    public string? SellerPhoneNumber { get; set; }
}
