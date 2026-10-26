using System;
using System.Collections.Generic;
using System.Text;

using CampusX.Enums;

namespace CampusX.Entities
{
    public class Report
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ReporterId { get; set; }
        public Guid? TargetProductId { get; set; }
        public Guid? TargetUserId { get; set; }
        public string Reason { get; set; } = string.Empty;
        public ReportStatus Status { get; set; } = ReportStatus.Pending;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public ApplicationUser Reporter { get; set; } = null!;
        public Product? TargetProduct { get; set; }
        public ApplicationUser? TargetUser { get; set; }

        public void Review(ReportStatus status)
        {
            Status = status;
        }
    }
}