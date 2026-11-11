using CampusX.Enums;

namespace CampusX.Models;

public class PromotionCreditPurchase
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BuyerId { get; set; }
    public PromotionPackage Package { get; set; }
    public int CreditsPurchased { get; set; }
    public decimal Amount { get; set; }
    public string ProviderReference { get; set; } = string.Empty;
    public DateTime PurchasedAt { get; set; } = DateTime.UtcNow;

    public ApplicationUser Buyer { get; set; } = null!;
}
