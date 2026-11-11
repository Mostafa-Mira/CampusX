using CampusX.Enums;

namespace CampusX.ViewModel;

public class RevenueDashboardViewModel
{
    public int AvailableCredits { get; set; }
    public decimal CommissionRatePercent { get; set; }
    public List<RevenuePackageViewModel> Packages { get; set; } = [];
    public List<PromotableProductViewModel> Products { get; set; } = [];
}

public class RevenuePackageViewModel
{
    public PromotionPackage Package { get; set; }
    public int Credits { get; set; }
    public decimal Price { get; set; }
}

public class PromotableProductViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public DateTime? PromotionExpiresAt { get; set; }
}
