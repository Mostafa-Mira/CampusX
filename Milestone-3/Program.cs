using CampusX.Data.DbContexts;
using CampusX.Data.Development;
using CampusX.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;

var provisionTester = args.Contains(DevelopmentTesterProvisioner.Command, StringComparer.Ordinal);
var provisionSharedTester = args.Contains(DevelopmentTesterProvisioner.SharedCommand, StringComparer.Ordinal);
if (provisionTester && provisionSharedTester)
    throw new InvalidOperationException("Choose one tester provisioning command.");

var builder = WebApplication.CreateBuilder(args.Where(arg => arg != DevelopmentTesterProvisioner.Command &&
    arg != DevelopmentTesterProvisioner.SharedCommand).ToArray());
var (sharedTesterMode, sharedTesterConnection) =
    builder.ConfigureCampusXTeamTester(provisionTester || provisionSharedTester);

// Service registration
builder.Services.AddControllersWithViews();
builder.Services.AddAutoMapper(typeof(Program));
builder.Services.AddCampusXIdentity(builder.Configuration);

builder.Services.AddCampusXRecoveryRateLimits();
builder.Services.AddCampusXAuthorization();

builder.Services.AddDbContext<CampusXDbContext>(options =>
    options.UseSqlServer(sharedTesterConnection ?? builder.Configuration.GetConnectionString("CampusXDb")));

builder.Services.AddCampusXBusinessServices(builder.Configuration);

// Application startup
var app = builder.Build();

if (provisionTester || provisionSharedTester)
{
    Environment.ExitCode = await DevelopmentTesterProvisioner.RunAsync(app, shared: provisionSharedTester);
    await app.DisposeAsync();
    return;
}

app.InitializeCampusXDevelopmentDatabase(sharedTesterMode);

// Request pipeline
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
var mapper = app.Services.GetRequiredService<AutoMapper.IMapper>();
mapper.ConfigurationProvider.AssertConfigurationIsValid();
