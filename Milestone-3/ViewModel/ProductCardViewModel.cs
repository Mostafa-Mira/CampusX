namespace CampusX.ViewModels
{
    public class ProductCardViewModel
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public decimal Price { get; set; }

        public string CategoryName { get; set; } = string.Empty;

        public string College { get; set; } = string.Empty;

        public string PhoneNumber { get; set; } = string.Empty;

        public string ImageUrl { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string SellerName { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
    }
}
