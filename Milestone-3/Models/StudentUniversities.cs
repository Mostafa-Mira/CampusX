namespace CampusX.Models;

// A registration choice, not evidence of university membership or an email-domain allowlist.
public static class StudentUniversities
{
    public const string Cairo = "Cairo University";
    public const string ClaimType = "campusx:university";
    public static IReadOnlyList<string> Names { get; } = Array.AsReadOnly(new[]
    {
        "Cairo University", "Ain Shams University", "Alexandria University", "Helwan University"
    });

    public static string NormalizeEmail(string? email) => (email ?? string.Empty).Trim().ToLowerInvariant();

    public static bool IsCairoStudentEmail(string? email) =>
        System.Text.RegularExpressions.Regex.IsMatch(NormalizeEmail(email), @"\A[0-9]{14}@stud\.cu\.edu\.eg\z");

    internal static bool HasVerifiedEmail(string? email, string? verifiedEmail, bool confirmed) =>
        confirmed && IsCairoStudentEmail(email) && verifiedEmail == NormalizeEmail(email);
}
