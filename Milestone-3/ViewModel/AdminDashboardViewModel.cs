namespace CampusX.ViewModel;

public class AdminDashboardViewModel
{
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int TotalProducts { get; set; }
    public int AvailableProducts { get; set; }
    public int SoldProducts { get; set; }
    public int PendingReports { get; set; }

    public List<AdminRecentReportViewModel> RecentReports { get; set; } = [];
}

public class PublicDashboardViewModel
{
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int TotalProducts { get; set; }
    public int AvailableProducts { get; set; }
    public int SoldProducts { get; set; }
    public int PendingReports { get; set; }
}

public class AdminRecentReportViewModel
{
    public Guid Id { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string ReporterName { get; set; } = string.Empty;
    public string TargetName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
