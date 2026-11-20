using CampusX.Data.DbContexts;
using CampusX.Enums;
using CampusX.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using System.Security.Claims;

namespace CampusX.Models;

// Business operations for student accounts. Controllers do not access DbContext directly.
public class ApplicationUserBL
{
    private readonly CampusXDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public ApplicationUserBL(CampusXDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public Task<ApplicationUser> RegisterAsync(ApplicationUser user, string password, string university)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("A password is required.", nameof(password));
        return CreateAccountAsync(user, university, () => _userManager.CreateAsync(user, password));
    }

    public Task<ApplicationUser> RegisterWithGoogleAsync(ApplicationUser user, UserLoginInfo login)
    {
        if (!StudentUniversities.IsCairoStudentEmail(user.UniversityEmail))
            throw new InvalidOperationException("Enter an eligible Cairo University student email.");
        return CreateAccountAsync(user, StudentUniversities.Cairo, () => _userManager.CreateAsync(user), login);
    }

    private async Task<ApplicationUser> CreateAccountAsync(ApplicationUser user, string university,
        Func<Task<IdentityResult>> create, UserLoginInfo? login = null)
    {
        if (!StudentUniversities.Names.Contains(university, StringComparer.Ordinal))
            throw new InvalidOperationException("Select one of the supported universities.");
        if (string.IsNullOrWhiteSpace(user.UniversityEmail))
            throw new InvalidOperationException("University email is required.");
        if (string.IsNullOrWhiteSpace(user.Email))
            throw new ArgumentException("Email is required.", nameof(user));
        user.Email = user.Email.Trim();
        if (await _userManager.FindByEmailAsync(user.Email) is not null)
            throw new InvalidOperationException("This email is already registered.");

        user.Role = UserRole.Student;
        user.UserName = user.Id.ToString();
        user.UniversityEmail = user.UniversityEmail.Trim();
        user.UniversityEmailConfirmed = false;
        user.EmailConfirmed = false;
        user.IsBlocked = false;
        // The account, its university choice and any provider link must persist together.
        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var result = await create();
            if (!result.Succeeded)
                throw new InvalidOperationException(login is null
                    ? string.Join(" ", result.Errors.Select(error => error.Description))
                    : "Could not create the account. If you already registered, log in and link Google.");
            if (login is not null && !(await _userManager.AddLoginAsync(user, login)).Succeeded)
                throw new InvalidOperationException("This Google account is already linked. Start again from login.");
            var claimResult = await _userManager.AddClaimAsync(user, new Claim(StudentUniversities.ClaimType, university));
            if (!claimResult.Succeeded)
                throw new InvalidOperationException("The account could not be created. Please try again.");
            await transaction.CommitAsync();
        }
        catch (DbUpdateException exception) when (exception.InnerException is SqlException { Number: 2601 or 2627 })
        {
            throw new InvalidOperationException(login is null ? "This email is already registered."
                : "This email or Google account is already registered. Start again from login.", exception);
        }
        return user;
    }

    public async Task<ApplicationUser?> AuthenticateAsync(string email, string password)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrEmpty(password))
            return null;

        var user = await _userManager.FindByEmailAsync(email.Trim());
        return user is { IsBlocked: false } && await _userManager.CheckPasswordAsync(user, password) ? user : null;
    }

    public ApplicationUser? GetById(Guid id) => _context.ApplicationUsers.Find(id);

    public List<ApplicationUser> GetStudents(Guid adminId)
    {
        RequireEligibleAdmin(_context, adminId);
        return _context.ApplicationUsers.Where(user => user.Role == UserRole.Student).ToList();
    }

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
        RequireEligibleAdmin(_context, adminId);

        var student = _context.ApplicationUsers.Find(studentId)
            ?? throw new InvalidOperationException("Student not found.");
        if (student.Role != UserRole.Student)
            throw new InvalidOperationException("Only student accounts can be managed.");

        if (blocked) student.BlockAccount();
        else student.UnblockAccount();
        _context.SaveChanges();
    }

    internal static void RequireEligibleAdmin(CampusXDbContext context, Guid adminId) =>
        RequireEligibleRole(context, adminId, UserRole.Admin);

    internal static void RequireEligibleRole(CampusXDbContext context, Guid userId, UserRole role,
        DevelopmentTesterAccess? testerAccess = null)
    {
        var account = context.ApplicationUsers.AsNoTracking()
            .Where(user => user.Id == userId && user.Role == role && !user.IsBlocked)
            .Select(user => new { user.Email, user.UniversityEmail, user.VerifiedUniversityEmail, user.UniversityEmailConfirmed })
            .SingleOrDefault();

        if (account is null || (!StudentUniversities.HasVerifiedEmail(
            account.UniversityEmail, account.VerifiedUniversityEmail, account.UniversityEmailConfirmed) &&
            testerAccess?.IsAllowed(userId, role, account.Email) != true))
            throw new UnauthorizedAccessException($"Verified {role.ToString().ToLowerInvariant()} access is required.");
    }
}
