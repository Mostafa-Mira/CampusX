using CampusX.Data.DbContexts;
using CampusX.Data.Development;
using CampusX.Data.Seed;
using CampusX.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<CampusXDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("CampusXDb")));
builder.Services.AddScoped<ApplicationUserBL>();
builder.Services.AddScoped<CategoryBL>();
builder.Services.AddScoped<ProductBL>();
builder.Services.AddScoped<ReportBL>();
builder.Services.AddScoped<ContactInquiryBL>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<CampusXDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        // In Development, create the database if needed and bring it up to the current schema.
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
app.UseAuthorization();
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
app.Run();
