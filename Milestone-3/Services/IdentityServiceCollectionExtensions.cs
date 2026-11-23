using CampusX.Data.DbContexts;
using CampusX.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;

namespace CampusX.Services;

internal static class IdentityServiceCollectionExtensions
{
    internal static void AddCampusXIdentity(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Stores.MaxLengthForKeys = 128;
                // Preserve existing password acceptance until a password policy is approved separately.
                options.Password.RequiredLength = 1;
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
                // Marketplace eligibility uses separate university verification, never personal email confirmation.
            })
            .AddEntityFrameworkStores<CampusXDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders()
            .AddClaimsPrincipalFactory<CampusXUserClaimsPrincipalFactory>();
        services.Configure<DataProtectionTokenProviderOptions>(options => options.TokenLifespan = PasswordRecoveryBL.Lifetime);

        var authentication = services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = IdentityConstants.ApplicationScheme;
                options.DefaultChallengeScheme = IdentityConstants.ApplicationScheme;
                options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
            });
        authentication.AddIdentityCookies();
        var googleClientId = configuration["Authentication:Google:ClientId"];
        var googleClientSecret = configuration["Authentication:Google:ClientSecret"];
        if (!string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleClientSecret))
        {
            authentication.AddGoogle(options =>
            {
                options.ClientId = googleClientId;
                options.ClientSecret = googleClientSecret;
                options.CallbackPath = "/signin-google";
                options.SignInScheme = IdentityConstants.ExternalScheme;
                options.UsePkce = true;
                options.ClaimActions.MapJsonKey("google:email_verified", "email_verified", "boolean");
                options.Events.OnRemoteFailure = async context =>
                {
                    context.HandleResponse();
                    await context.HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
                    var session = await context.HttpContext.AuthenticateAsync(IdentityConstants.ApplicationScheme);
                    var action = session.Succeeded ? "GoogleLink" : "Login";
                    context.Response.Redirect($"{context.Request.PathBase}/Account/{action}?googleError=true");
                };
            });
        }
        services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = "/Account/Login";
                options.AccessDeniedPath = "/Account/AccessDenied";
                options.Cookie.Name = "CampusX.Auth";
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                options.ExpireTimeSpan = TimeSpan.FromHours(8);
                options.SlidingExpiration = true;
            });
    }
}
