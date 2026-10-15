using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CampusX.Domain.Enums;

namespace CampusX.Domain.Entities;

public class Report
{
    public Guid Id { get; private set; }
    public Guid ReporterId { get; private set; }
    public Guid? TargetProductId { get; private set; }
    public Guid? TargetUserId { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public ReportStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Navigation Properties
    public ApplicationUser Reporter { get; private set; } = null!;
    public Product? TargetProduct { get; private set; }
    public ApplicationUser? TargetUser { get; private set; }

    private Report() { }

    public Report(Guid reporterId, string reason, Guid? targetProductId = null, Guid? targetUserId = null)
    {
        if (targetProductId == null && targetUserId == null)
            throw new ArgumentException("A report must target either a product or a user.");

        Id = Guid.NewGuid();
        ReporterId = reporterId;
        Reason = reason;
        TargetProductId = targetProductId;
        TargetUserId = targetUserId;
        Status = ReportStatus.Pending;
        CreatedAt = DateTime.UtcNow;
    }

    public void Review(ReportStatus newStatus)
    {
        Status = newStatus;
    }
}
