using CampusX.Data.DbContexts;
using CampusX.Data.Development;
using CampusX.Data.Seed;
using CampusX.Models;
using CampusX.Options;
using CampusX.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// تسجيل الـ AutoMapper هنا
builder.Services.AddAutoMapper(typeof(Program));

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.Cookie.Name = "CampusX.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

builder.Services.AddDbContext<CampusXDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("CampusXDb")));

builder.Services.AddScoped<ApplicationUserBL>();
builder.Services.AddScoped<CategoryBL>();
builder.Services.AddScoped<ProductBL>();
builder.Services.AddScoped<ReportBL>();
builder.Services.AddScoped<ContactInquiryBL>();

builder.Services.Configure<RevenueOptions>(builder.Configuration.GetSection(RevenueOptions.SectionName));
builder.Services.AddScoped<IPaymentProvider, SimulatedPaymentProvider>();
builder.Services.AddScoped<RevenueBL>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
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
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
// للتأكد من صحة الـ Mappings
var mapper = app.Services.GetRequiredService<AutoMapper.IMapper>();
mapper.ConfigurationProvider.AssertConfigurationIsValid();