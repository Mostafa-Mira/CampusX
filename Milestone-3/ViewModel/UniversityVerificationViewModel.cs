using System.ComponentModel.DataAnnotations;

namespace CampusX.ViewModel;

public class UniversityVerificationViewModel
{
    public string MaskedEmail { get; set; } = string.Empty;
    public bool IsEligible { get; set; }
    public bool IsConfirmed { get; set; }
    public bool HasActiveCode { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public int ResendAfterSeconds { get; set; }
    public string? Notice { get; set; }

    [Required]
    [RegularExpression(@"\A[0-9]{8}\z", ErrorMessage = "Enter the 8-digit verification code.")]
    public string Code { get; set; } = string.Empty;
}

public class ChangeUniversityEmailViewModel
{
    [Required, EmailAddress, StringLength(256)]
    [Display(Name = "University email")]
    public string UniversityEmail { get; set; } = string.Empty;
}
