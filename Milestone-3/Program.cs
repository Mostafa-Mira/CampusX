using CampusX.Data.DbContexts;
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
