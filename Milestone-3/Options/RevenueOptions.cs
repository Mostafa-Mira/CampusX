namespace CampusX.Options;

public class RevenueOptions
{
    public const string SectionName = "Revenue";

    public decimal BasicPackagePrice { get; set; } = 30m;
    public decimal StandardPackagePrice { get; set; } = 45m;
    public decimal PremiumPackagePrice { get; set; } = 80m;
    public int PromotionDurationDays { get; set; } = 7;
    public decimal CommissionRatePercent { get; set; } = 10m;
}
