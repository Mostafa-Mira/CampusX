using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace CampusX.ViewModel;

public class GoogleRegistrationViewModel
{
    [Required, StringLength(100)]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(256)]
    [Display(Name = "University email")]
    public string UniversityEmail { get; set; } = string.Empty;

    [BindNever]
    public string GoogleEmail { get; set; } = string.Empty;

    public string Challenge { get; set; } = string.Empty;
}

public class GoogleLinkViewModel
{
    [BindNever]
    public bool IsLinked { get; set; }

    [BindNever]
    public string? GoogleEmail { get; set; }

    [Required, DataType(DataType.Password)]
    [Display(Name = "CampusX password")]
    public string Password { get; set; } = string.Empty;

    public string Challenge { get; set; } = string.Empty;
}

