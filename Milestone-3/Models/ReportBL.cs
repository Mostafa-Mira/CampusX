using CampusX.Data.DbContexts;
using CampusX.Enums;
using Microsoft.EntityFrameworkCore;

namespace CampusX.Models;

public class ReportBL
{
    private readonly CampusXDbContext _context;

    public ReportBL(CampusXDbContext context) => _context = context;

    public Report AddForProduct(Guid reporterId, Guid productId, string reason)
    {
        RequireReporter(reporterId, reason);
        if (!_context.Products.Any(product => product.Id == productId && !product.IsDeleted))
            throw new InvalidOperationException("Product not found.");

        var report = new Report
        {
            ReporterId = reporterId,
            TargetProductId = productId,
            Reason = reason.Trim()
        };
        _context.Reports.Add(report);
        _context.SaveChanges();
        return report;
    }

    public Report AddForUser(Guid reporterId, Guid targetUserId, string reason)
    {
        RequireReporter(reporterId, reason);
        if (reporterId == targetUserId ||
            !_context.ApplicationUsers.Any(user => user.Id == targetUserId))
            throw new InvalidOperationException("Invalid reported user.");

        var report = new Report
        {
            ReporterId = reporterId,
            TargetUserId = targetUserId,
            Reason = reason.Trim()
        };
        _context.Reports.Add(report);
        _context.SaveChanges();
        return report;
    }

    public List<Report> GetAll(Guid adminId)
    {
        RequireAdmin(adminId);
        return _context.Reports.AsNoTracking()
            .Include(report => report.Reporter)
            .Include(report => report.TargetProduct)
            .Include(report => report.TargetUser)
            .OrderByDescending(report => report.CreatedAt)
            .ToList();
    }

    public void Handle(Guid adminId, Guid reportId)
    {
        RequireAdmin(adminId);
        var report = _context.Reports.Find(reportId)
            ?? throw new InvalidOperationException("Report not found.");
        report.Review(ReportStatus.Reviewed);
        _context.SaveChanges();
    }

    private void RequireReporter(Guid reporterId, string reason)
    {
        var reporter = _context.ApplicationUsers.Find(reporterId);
        if (reporter?.Role != UserRole.Student || reporter.IsBlocked)
            throw new UnauthorizedAccessException("An active student account is required.");
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("A reason is required.", nameof(reason));
    }

    private void RequireAdmin(Guid adminId)
    {
        if (_context.ApplicationUsers.Find(adminId)?.Role != UserRole.Admin)
            throw new UnauthorizedAccessException("Admin access is required.");
    }
}
