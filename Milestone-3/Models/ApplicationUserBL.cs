using CampusX.Data.DbContexts;
using CampusX.Enums;

namespace CampusX.Models;

// Business operations for student accounts. Controllers do not access DbContext directly.
public class ApplicationUserBL
{
    private readonly CampusXDbContext _context;

    public ApplicationUserBL(CampusXDbContext context) => _context = context;

    public ApplicationUser Register(ApplicationUser user, string password)
    {
        if (string.IsNullOrWhiteSpace(user.Email))
            throw new ArgumentException("Email is required.", nameof(user));
        user.Email = user.Email.Trim();
        if (_context.ApplicationUsers.Any(existing => existing.Email == user.Email))
            throw new InvalidOperationException("This email is already registered.");

        user.Role = UserRole.Student;
        user.SetPassword(password);
        _context.ApplicationUsers.Add(user);
        _context.SaveChanges();
        return user;
    }

    public ApplicationUser? Authenticate(string email, string password)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrEmpty(password))
            return null;

        var user = _context.ApplicationUsers
            .FirstOrDefault(candidate => candidate.Email == email.Trim());
        return user is { IsBlocked: false } && user.VerifyPassword(password) ? user : null;
    }

    public ApplicationUser? GetById(Guid id) => _context.ApplicationUsers.Find(id);

    public List<ApplicationUser> GetStudents() =>
        _context.ApplicationUsers.Where(user => user.Role == UserRole.Student).ToList();

    public void UpdateProfile(Guid studentId, string fullName, string? phoneNumber,
        string collegeName, string? profileImageUrl)
    {
        var student = _context.ApplicationUsers.Find(studentId)
            ?? throw new InvalidOperationException("Student not found.");
        if (student.Role != UserRole.Student)
            throw new InvalidOperationException("Only student profiles can be updated here.");

        student.UpdateProfile(fullName, phoneNumber, collegeName, profileImageUrl);
        _context.SaveChanges();
    }

    public void SetBlocked(Guid adminId, Guid studentId, bool blocked)
    {
        var admin = _context.ApplicationUsers.Find(adminId);
        if (admin?.Role != UserRole.Admin)
            throw new UnauthorizedAccessException("Admin access is required.");

        var student = _context.ApplicationUsers.Find(studentId)
            ?? throw new InvalidOperationException("Student not found.");
        if (student.Role != UserRole.Student)
            throw new InvalidOperationException("Only student accounts can be managed.");

        if (blocked) student.BlockAccount();
        else student.UnblockAccount();
        _context.SaveChanges();
    }
}
