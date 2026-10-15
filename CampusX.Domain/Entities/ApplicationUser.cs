using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CampusX.Domain.Enums;

namespace CampusX.Domain.Entities;

public class ApplicationUser
{
    public Guid Id { get; private set; }
    public string FullName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string? PhoneNumber { get; private set; }
    public string CollegeName { get; private set; } = string.Empty;
    public string? ProfileImageUrl { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public bool IsBlocked { get; private set; }
    public UserRole Role { get; private set; }

    // Navigation Properties
    public ICollection<Product> Products { get; private set; } = new List<Product>();
    public ICollection<Report> SubmittedReports { get; private set; } = new List<Report>();
    public ICollection<ContactInquiry> BuyerInquiries { get; private set; } = new List<ContactInquiry>();
    public ICollection<ContactInquiry> SellerInquiries { get; private set; } = new List<ContactInquiry>();

    private ApplicationUser() { }

    public ApplicationUser(string fullName, string email, string collegeName, UserRole role = UserRole.Student, string? phoneNumber = null, string? profileImageUrl = null)
    {
        Id = Guid.NewGuid();
        FullName = fullName;
        Email = email;
        CollegeName = collegeName;
        Role = role;
        PhoneNumber = phoneNumber;
        ProfileImageUrl = profileImageUrl;
        CreatedAt = DateTime.UtcNow;
        IsBlocked = false;
    }

    // Domain Behaviors
    public void UpdateProfile(string fullName, string? phoneNumber, string collegeName, string? profileImageUrl)
    {
        FullName = fullName;
        PhoneNumber = phoneNumber;
        CollegeName = collegeName;
        ProfileImageUrl = profileImageUrl;
    }

    public void BlockAccount() => IsBlocked = true;
    public void UnblockAccount() => IsBlocked = false;
}
