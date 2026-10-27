namespace CampusX.ViewModels
{
    public class HomeIndexViewModel
    {
        public string Heading { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public List<ProductCardViewModel> Products { get; set; }
            = new List<ProductCardViewModel>();
    }
}