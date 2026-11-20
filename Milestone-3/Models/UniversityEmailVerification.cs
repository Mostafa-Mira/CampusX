namespace CampusX.Models;

public class UniversityEmailVerification
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string Recipient { get; set; } = string.Empty;
    public string CodeHash { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime? ConsumedAt { get; set; }
    public bool IsActive { get; set; }
}

// Separate events preserve the rolling attempt budget across resends and email changes.
public class UniversityVerificationAttempt
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string Recipient { get; set; } = string.Empty;
    public DateTime AttemptedAt { get; set; }
}
