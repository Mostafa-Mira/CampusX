using CampusX.Data.DbContexts;
using CampusX.Data.Development;
using CampusX.Data.Seed;
using CampusX.Enums;
using CampusX.Models;
using CampusX.Options;
using CampusX.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.Extensions.FileProviders;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// تسجيل الـ AutoMapper هنا
builder.Services.AddAutoMapper(typeof(Program));

builder.Services.AddIdentityCore<ApplicationUser>(options =>
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
builder.Services.Configure<DataProtectionTokenProviderOptions>(options => options.TokenLifespan = PasswordRecoveryBL.Lifetime);

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
    {
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var wait))
            context.HttpContext.Response.Headers.RetryAfter =
                ((int)Math.Ceiling(wait.TotalSeconds)).ToString(System.Globalization.CultureInfo.InvariantCulture);
        await context.HttpContext.Response.WriteAsync("Too many recovery attempts. Wait a few minutes and try again.", cancellationToken);
    };
    foreach (var (policy, limit) in new[] { (PasswordRecoveryQueue.RequestPolicy, 10), (PasswordRecoveryQueue.ResetPolicy, 15) })
    {
        options.AddPolicy(policy, context => RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = limit,
                Window = TimeSpan.FromMinutes(5),
                QueueLimit = 0
            }));
    }
});

var authentication = builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = IdentityConstants.ApplicationScheme;
        options.DefaultChallengeScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    });
authentication.AddIdentityCookies();
var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
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
builder.Services.ConfigureApplicationCookie(options =>
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

builder.Services.AddAuthorization(options =>
{
    var marketplace = MarketplaceAccessHandler.CreateMarketplacePolicy();
    // Cover both plain [Authorize] endpoints and routes with no explicit authorization attribute.
    options.DefaultPolicy = marketplace;
    options.FallbackPolicy = marketplace;
    options.AddPolicy(MarketplaceAccessPolicies.Marketplace, marketplace);
    options.AddPolicy(MarketplaceAccessPolicies.Student, MarketplaceAccessHandler.CreateMarketplacePolicy(UserRole.Student));
    options.AddPolicy(MarketplaceAccessPolicies.Admin, MarketplaceAccessHandler.CreateMarketplacePolicy(UserRole.Admin));
    options.AddPolicy(MarketplaceAccessPolicies.AuthenticatedOnly, policy =>
        policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme).RequireAuthenticatedUser());
});
builder.Services.AddScoped<MarketplaceAccessHandler>();
builder.Services.AddScoped<IAuthorizationHandler>(services => services.GetRequiredService<MarketplaceAccessHandler>());
builder.Services.AddSingleton<IAuthorizationMiddlewareResultHandler, MarketplaceAuthorizationResultHandler>();

builder.Services.AddDbContext<CampusXDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("CampusXDb")));

builder.Services.AddScoped<ApplicationUserBL>();
builder.Services.AddScoped<CategoryBL>();
builder.Services.AddScoped<ProductBL>();
builder.Services.AddScoped<ReportBL>();
builder.Services.AddScoped<ContactInquiryBL>();
builder.Services.Configure<EmailSmtpOptions>(builder.Configuration.GetSection(EmailSmtpOptions.SectionName));
builder.Services.AddScoped<GmailUniversityEmailSender>();
builder.Services.AddScoped<IUniversityEmailSender>(services => services.GetRequiredService<GmailUniversityEmailSender>());
builder.Services.AddScoped<IPasswordRecoveryEmailSender>(services => services.GetRequiredService<GmailUniversityEmailSender>());
builder.Services.AddScoped<PasswordRecoveryBL>();
builder.Services.AddSingleton<PasswordRecoveryQueue>();
builder.Services.AddHostedService(services => services.GetRequiredService<PasswordRecoveryQueue>());
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<UniversityEmailVerificationBL>();

builder.Services.Configure<RevenueOptions>(builder.Configuration.GetSection(RevenueOptions.SectionName));
builder.Services.AddScoped<IPaymentProvider, SimulatedPaymentProvider>();
builder.Services.AddScoped<RevenueBL>();

var app = builder.Build();

// Shared team databases must be migrated separately and must not receive development seed data.
var useSharedDatabase = app.Configuration.GetValue<bool>("DatabaseStartup:UseSharedDatabase");
if (app.Environment.IsDevelopment() && !useSharedDatabase)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<CampusXDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        db.Database.Migrate();
        SampleProductSeeder.Seed(db);
    }
    catch (Exception exception)
    {
        logger.LogCritical(exception,
            "CampusX database initialization failed. Check that SQL Server is running and the CampusXDb connection string is valid.");
        throw new InvalidOperationException(
            "CampusX could not initialize its Development database. Verify SQL Server and the CampusXDb connection string.",
            exception);
    }

    DevelopmentTestDataSeeder.TrySeed(
        db,
        app.Configuration,
        app.Environment,
        scope.ServiceProvider.GetRequiredService<ILogger<Program>>());
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// Only UI assets are public. Each provider is rooted inside its own directory so traversal cannot
// expose product uploads through an apparently public /css or /js URL.
foreach (var directory in new[] { "css", "js" })
{
    var provider = new PhysicalFileProvider(Path.Combine(app.Environment.WebRootPath, directory));
    app.Lifetime.ApplicationStopped.Register(provider.Dispose);
    app.UseStaticFiles(new StaticFileOptions { RequestPath = "/" + directory, FileProvider = provider });
}

app.UseRouting();

app.UseAuthentication();
app.Use(async (context, next) =>
{
    context.Response.Headers.CacheControl = "private, no-store";
    await next();
});
app.UseAuthorization();
app.UseRateLimiter();
// Remaining files, including /images/products, pass the same eligibility policy as MVC routes.
app.UseStaticFiles();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
// للتأكد من صحة الـ Mappings
var mapper = app.Services.GetRequiredService<AutoMapper.IMapper>();
mapper.ConfigurationProvider.AssertConfigurationIsValid();
