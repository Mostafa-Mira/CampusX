using CampusX.Enums;

namespace CampusX.ViewModels
{
    public class MyProductsViewModel
    {
        public List<MyProductItemViewModel> Items { get; set; } = new List<MyProductItemViewModel>();

        public int AvailableCount => Items.Count(item => item.Status == ProductStatus.Available);

        public int SoldCount => Items.Count(item => item.Status == ProductStatus.Sold);
    }

    public class MyProductItemViewModel
    {
        public ProductCardViewModel Card { get; set; } = new ProductCardViewModel();

        public ProductStatus Status { get; set; }
    }
}
