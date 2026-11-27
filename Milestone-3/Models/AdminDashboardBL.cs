using CampusX.Data.DbContexts;
using CampusX.Enums;
using CampusX.ViewModel;
using Microsoft.EntityFrameworkCore;

namespace CampusX.Models;

public class AdminDashboardBL
{
    private readonly CampusXDbContext _context;

    public AdminDashboardBL(CampusXDbContext context)
    {
        _context = context;
    }

    public async Task<AdminDashboardViewModel> GetOverviewAsync()
    {
        var reports = await _context.Reports
            .AsNoTracking()
            .OrderByDescending(report => report.CreatedAt)
            .Take(5)
            .Select(report => new AdminRecentReportViewModel
            {
                Id = report.Id,
                Reason = report.Reason,
                ReporterName = report.Reporter.FullName,
                TargetName = report.TargetProduct != null
                    ? report.TargetProduct.Name
                    : report.TargetUser != null
                        ? report.TargetUser.FullName
                        : "Unknown target",
                CreatedAt = report.CreatedAt
            })
            .ToListAsync();

        return new AdminDashboardViewModel
        {
            TotalUsers = await _context.ApplicationUsers.CountAsync(),
            ActiveUsers = await _context.ApplicationUsers
                .CountAsync(user => !user.IsBlocked),
            TotalProducts = await _context.Products
                .CountAsync(product => !product.IsDeleted),
            AvailableProducts = await _context.Products
                .CountAsync(product =>
                    !product.IsDeleted &&
                    product.Status == ProductStatus.Available),
            SoldProducts = await _context.Products
                .CountAsync(product =>
                    !product.IsDeleted &&
                    product.Status == ProductStatus.Sold),
            PendingReports = await _context.Reports
                .CountAsync(report => report.Status == ReportStatus.Pending),
            RecentReports = reports
        };
    }

    public async Task<PublicDashboardViewModel> GetPublicOverviewAsync() => new()
    {
        TotalUsers = await _context.ApplicationUsers.CountAsync(),
        ActiveUsers = await _context.ApplicationUsers.CountAsync(user => !user.IsBlocked),
        TotalProducts = await _context.Products.CountAsync(product => !product.IsDeleted),
        AvailableProducts = await _context.Products.CountAsync(product =>
            !product.IsDeleted && product.Status == ProductStatus.Available),
        SoldProducts = await _context.Products.CountAsync(product =>
            !product.IsDeleted && product.Status == ProductStatus.Sold),
        PendingReports = await _context.Reports.CountAsync(report => report.Status == ReportStatus.Pending)
    };
}
