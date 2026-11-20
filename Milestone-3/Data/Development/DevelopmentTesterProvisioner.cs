using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text;
using CampusX.Data.DbContexts;
using CampusX.Enums;
using CampusX.Models;
using CampusX.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CampusX.Data.Development;

internal static class DevelopmentTesterProvisioner
{
    internal const string Command = "--provision-tester";
    internal const string SharedCommand = "--provision-shared-tester";

    internal static async Task<int> RunAsync(WebApplication app, bool shared)
    {
        if (shared)
        {
            if (!DevelopmentTesterAccess.IsSharedMode(app.Environment, app.Configuration) ||
                !DevelopmentTesterAccess.AllowsSharedProvisioning(app.Configuration) ||
                !DevelopmentTesterAccess.UsesApprovedSharedDatabase(app.Configuration, app.Configuration.GetConnectionString("CampusXDb")))
                return Fail("Shared tester provisioning requires the Development Team Test profile, private opt-in, provisioning permission and the approved database.");

            Console.Write("Type the configured database name to confirm the provisioning target: ");
            if (!string.Equals(Console.ReadLine()?.Trim(), app.Configuration["DevelopmentTesterAccess:SharedDatabase"]?.Trim(), StringComparison.Ordinal))
                return Fail("Database confirmation did not match. No account was created.");
        }
        else if (!DevelopmentTesterAccess.IsEnabled(app.Environment, app.Configuration) ||
                 DevelopmentTesterAccess.IsSharedProfile(app.Environment, app.Configuration) ||
                 !DevelopmentTesterAccess.UsesLocalDatabase(app.Configuration))
            return Fail("Tester provisioning requires Development, enabled tester access and an isolated local CampusX database.");

        using var scope = app.Services.CreateScope();
        var access = scope.ServiceProvider.GetRequiredService<DevelopmentTesterAccess>();
        var db = scope.ServiceProvider.GetRequiredService<CampusXDbContext>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        Console.Write("Tester login email: ");
        var email = Console.ReadLine()?.Trim();
        if (email is null || email.Length > 256 || !new EmailAddressAttribute().IsValid(email) || !access.IsAllowlisted(email))
            return Fail("Enter a valid login email on the private tester allowlist.");

        Console.Write("Display name: ");
        var name = Console.ReadLine()?.Trim();
        if (string.IsNullOrWhiteSpace(name))
            return Fail("A display name is required.");

        var password = ReadPassword("Password (at least 12 characters): ");
        if (string.IsNullOrWhiteSpace(password) || password.Length < 12 || password != ReadPassword("Confirm password: "))
            return Fail("Use a password of at least 12 characters and enter it twice.");

        try
        {
            if (shared && !DevelopmentTesterAccess.UsesApprovedSharedDatabase(app.Configuration, db.Database.GetConnectionString()))
                return Fail("The effective SQL connection no longer matches the approved shared target.");
            if (!await db.Database.CanConnectAsync() || (await db.Database.GetPendingMigrationsAsync()).Any())
                return Fail("Use the approved database with the project's migrations already applied.");
            if (await users.FindByEmailAsync(email) is not null)
                return Fail("This email already belongs to an account. Use a separate tester email.");

            await using var transaction = await db.Database.BeginTransactionAsync();
            var user = new ApplicationUser { Email = email, FullName = name, Role = UserRole.Student };
            user.UserName = user.Id.ToString();
            if (!(await users.CreateAsync(user, password)).Succeeded ||
                !(await users.AddClaimAsync(user, new Claim(DevelopmentTesterAccess.ClaimType, DevelopmentTesterAccess.ClaimValue))).Succeeded)
                return Fail("The tester account could not be created. No account changes were saved.");

            await transaction.CommitAsync();
            Console.WriteLine("Student tester account created. Sign in with the regular Login form.");
            return 0;
        }
        catch (DbUpdateException)
        {
            return Fail("The database rejected the tester account. Check that the email is unused and the local schema is current.");
        }
        catch (System.Data.Common.DbException)
        {
            return Fail("The configured database could not be used. Check that SQL Server is reachable and its schema is current.");
        }
    }

    private static string? ReadPassword(string prompt)
    {
        Console.Write(prompt);
        if (Console.IsInputRedirected)
            return Console.ReadLine();

        var password = new StringBuilder();
        while (true)
        {
            var key = Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Enter)
                break;
            if (key.Key == ConsoleKey.Backspace && password.Length > 0)
                password.Length--;
            else if (!char.IsControl(key.KeyChar))
                password.Append(key.KeyChar);
        }
        Console.WriteLine();
        return password.ToString();
    }

    private static int Fail(string message)
    {
        Console.Error.WriteLine(message);
        return 1;
    }
}
