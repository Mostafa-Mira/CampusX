using CampusX.Enums;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace CampusX.Models
{
    public class ApplicationUser
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        // Only a password hash is persisted; never store a plaintext password.
        public string PasswordHash { get; private set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string CollegeName { get; set; } = string.Empty;
        public string? ProfileImageUrl { get; set; }
        public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
        public bool IsBlocked { get; set; } = false;
        public UserRole Role { get; set; } = UserRole.Student;

        // Navigation Properties (Relations)
        public ICollection<Product> Products { get; set; } = new List<Product>();
        public ICollection<Report> ReportsSubmitted { get; set; } = new List<Report>();
        public ICollection<ContactInquiry> BuyerInquiries { get; set; } = new List<ContactInquiry>();

        // Behaviors
        public void SetPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("A password is required.", nameof(password));

            PasswordHash = new PasswordHasher<ApplicationUser>().HashPassword(this, password);
        }

        public bool VerifyPassword(string password) =>
            !string.IsNullOrEmpty(PasswordHash) &&
            new PasswordHasher<ApplicationUser>()
                .VerifyHashedPassword(this, PasswordHash, password) != PasswordVerificationResult.Failed;

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
}