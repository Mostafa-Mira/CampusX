using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace CampusX.ViewModel
{
    public class AddProductViewModel
    {
        [Required(ErrorMessage = "Product name is required.")]
        [Display(Name = "Product Name")]
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Product description is required.")]
        [Display(Name = "Description")]
        [StringLength(300, ErrorMessage = "Description cannot exceed 300 characters for better card display.")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Price is required.")]
        [Range(1, 1000000, ErrorMessage = "Please enter a valid price greater than zero.")]
        [Display(Name = "Price")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Category is required.")]
        [Display(Name = "Category")]
        public string Category { get; set; } = string.Empty;

        // تم إلغاء الإجبار (Required) لتصبح اختيارية ولو تركت فارغة تسجل General
        [Display(Name = "College")]
        public string? College { get; set; }

        [Required(ErrorMessage = "Phone number is required.")]
        [Display(Name = "Phone Number")]
        [RegularExpression(@"^01[0125][0-9]{8}$", ErrorMessage = "Please enter a valid Egyptian mobile number starting with 010, 011, 012, or 015 (11 digits).")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Display(Name = "Product Image")]
        public IFormFile? ImageFile { get; set; }
    }
}
