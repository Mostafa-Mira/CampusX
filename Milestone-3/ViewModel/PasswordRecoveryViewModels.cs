using System.ComponentModel.DataAnnotations;

namespace CampusX.ViewModel;

public sealed class ForgotPasswordViewModel
{
    [Required, EmailAddress, StringLength(256)]
    [Display(Name = "Login email")]
    public string Email { get; set; } = string.Empty;
}

public sealed class ResetPasswordViewModel
{
    [Required, StringLength(4096)]
    public string Ticket { get; set; } = string.Empty;

    // Presentation only. The protected ticket decides whether the code is required.
    public bool NeedsUniversityCode { get; set; }

    [StringLength(32)]
    public string? UniversityCode { get; set; }

    [Required, StringLength(256), DataType(DataType.Password)]
    [Display(Name = "New password")]
    public string Password { get; set; } = string.Empty;

    [Required, DataType(DataType.Password), Compare(nameof(Password), ErrorMessage = "The passwords do not match.")]
    [Display(Name = "Confirm new password")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
