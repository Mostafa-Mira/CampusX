namespace CampusX.ViewModel  // تأكد إنها مفرد أو جمع حسب باقي الملفات عندك في الفولدر (غالباً CampusX.ViewModel أو CampusX.ViewModels)
{
    public class EditProductViewModel
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
        public decimal Price { get; set; }
        public string? Description { get; set; }
        // باقي الخصائص...
    }
}