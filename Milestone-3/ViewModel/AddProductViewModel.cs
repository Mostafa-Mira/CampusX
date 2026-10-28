using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace CampusX.ViewModel
{
    public class AddProductViewModel
    {
        [Required(ErrorMessage = "Product name is required.")]
        [Display(Name = "Product Name")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Product description is required.")]
        [Display(Name = "Description")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Price is required.")]
        [Range(1, 1000000, ErrorMessage = "Please enter a valid price greater than zero.")]
        [Display(Name = "Price")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Category or college is required.")]
        [Display(Name = "Category / College")]
        public string Category { get; set; }

        [Display(Name = "Product Image")]
        public IFormFile? ImageFile { get; set; }
    }
}