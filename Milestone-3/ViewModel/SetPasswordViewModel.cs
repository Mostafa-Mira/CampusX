using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace CampusX.ViewModel;

public sealed record AccountSettingsViewModel(bool GoogleAvailable, bool GoogleLinked, bool HasPassword, bool UniversityVerified);

public sealed class SetPasswordViewModel
{
    [BindNever, ValidateNever]
    public UniversityVerificationViewModel Verification { get; set; } = new();

    public string? Code { get; set; }

    [Required, DataType(DataType.Password)]
    [Display(Name = "New password")]
    public string NewPassword { get; set; } = string.Empty;

    [Required, DataType(DataType.Password), Compare(nameof(NewPassword), ErrorMessage = "The passwords do not match.")]
    [Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
