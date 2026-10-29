using CampusX.Data.DbContexts;
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
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("SampleProductSeeder");
    try
    {
        if (db.Database.CanConnect())
        {
            var pendingMigrations = db.Database.GetPendingMigrations().ToArray();
            if (pendingMigrations.Length == 0)
                SampleProductSeeder.Seed(db);
            else
                logger.LogWarning("Sample products were not seeded because the database has pending migrations. Apply them with dotnet ef database update.");
        }
    }
    catch (Exception exception)
    {
        logger.LogWarning(exception, "Sample product seeding was skipped. Make sure the database is available and migrated.");
    }
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
