using CampusX.Models;

namespace CampusX.ViewModels
{
    public class HomeIndexViewModel
    {
        public string Heading { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public List<ProductCardViewModel> Products { get; set; }
            = new List<ProductCardViewModel>();

        public string? SearchTerm { get; set; }

        public Guid? CategoryId { get; set; }

        public decimal? MinPrice { get; set; }

        public decimal? MaxPrice { get; set; }

        public string SortBy { get; set; } = "newest";

        public List<Category> Categories { get; set; } = new List<Category>();

        public string? ErrorMessage { get; set; }
    }
}

