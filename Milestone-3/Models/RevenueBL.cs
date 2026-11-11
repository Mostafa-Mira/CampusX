using CampusX.Data.DbContexts;
using CampusX.Enums;
using CampusX.Options;
using CampusX.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Data;

namespace CampusX.Models;

public sealed record CommissionQuote(decimal SalePrice, decimal CommissionAmount, decimal SellerProceeds);
public sealed record RevenueProductSummary(Guid Id, string Name, decimal Price, DateTime? PromotionExpiresAt);

public class RevenueBL
{
    private readonly CampusXDbContext _context;
    private readonly IPaymentProvider _paymentProvider;
    private readonly RevenueOptions _options;

    public RevenueBL(CampusXDbContext context, IPaymentProvider paymentProvider, IOptions<RevenueOptions> options)
    {
        _context = context;
        _paymentProvider = paymentProvider;
        _options = options.Value;
    }

    public int GetAvailableCredits(Guid buyerId)
    {
        RequireActiveStudent(buyerId);
        return GetAvailableCreditsCore(buyerId);
    }

    public PromotionCreditPurchase PurchaseCredits(Guid buyerId, PromotionPackage package)
    {
        RequireActiveStudent(buyerId);
        var (credits, amount) = GetPackage(package);
        var payment = _paymentProvider.Charge(buyerId, amount, $"CampusX {package} promotion package");
        if (!payment.IsApproved)
            throw new InvalidOperationException("The promotion package payment was not approved.");

        var purchase = new PromotionCreditPurchase
        {
            BuyerId = buyerId,
            Package = package,
            CreditsPurchased = credits,
            Amount = amount,
            ProviderReference = payment.Reference
        };
        _context.PromotionCreditPurchases.Add(purchase);
        _context.SaveChanges();
        return purchase;
    }

    public ProductPromotion ActivatePromotion(Guid ownerId, Guid productId)
    {
        if (_options.PromotionDurationDays <= 0)
            throw new InvalidOperationException("Promotion duration must be configured as a positive number of days.");

        using var transaction = _context.Database.BeginTransaction(IsolationLevel.Serializable);
        RequireActiveStudent(ownerId);

        var product = _context.Products.SingleOrDefault(product => product.Id == productId)
            ?? throw new InvalidOperationException("Product not found.");
        if (product.SellerId != ownerId || product.IsDeleted || product.Status != ProductStatus.Available)
            throw new UnauthorizedAccessException("Only the owner can promote an available product.");

        var now = DateTime.UtcNow;
        if (_context.ProductPromotions.Any(promotion => promotion.ProductId == productId && promotion.ExpiresAt > now))
            throw new InvalidOperationException("This product already has an active promotion.");
        if (GetAvailableCreditsCore(ownerId) < 1)
            throw new InvalidOperationException("Purchase promotion credits before promoting a product.");

        var promotion = new ProductPromotion
        {
            ProductId = productId,
            ActivatedAt = now,
            ExpiresAt = now.AddDays(_options.PromotionDurationDays)
        };
        _context.ProductPromotions.Add(promotion);
        _context.SaveChanges();
        transaction.Commit();
        return promotion;
    }


    public List<RevenueProductSummary> GetOwnedAvailableProducts(Guid ownerId)
    {
        RequireActiveStudent(ownerId);
        var now = DateTime.UtcNow;
        return _context.Products.AsNoTracking()
            .Where(product => product.SellerId == ownerId && !product.IsDeleted && product.Status == ProductStatus.Available)
            .OrderByDescending(product => product.CreatedAt)
            .Select(product => new RevenueProductSummary(
                product.Id,
                product.Name,
                product.Price,
                product.Promotions.Where(promotion => promotion.ExpiresAt > now)
                    .OrderByDescending(promotion => promotion.ActivatedAt)
                    .Select(promotion => (DateTime?)promotion.ExpiresAt)
                    .FirstOrDefault()))
            .ToList();
    }    public CommissionQuote CalculateCommission(decimal completedSalePrice)
    {
        if (completedSalePrice < 0)
            throw new ArgumentOutOfRangeException(nameof(completedSalePrice));
        if (_options.CommissionRatePercent < 0 || _options.CommissionRatePercent > 100)
            throw new InvalidOperationException("Commission rate must be between 0 and 100.");

        var commission = Math.Round(completedSalePrice * _options.CommissionRatePercent / 100m, 2, MidpointRounding.AwayFromZero);
        return new CommissionQuote(completedSalePrice, commission, completedSalePrice - commission);
    }

    private int GetAvailableCreditsCore(Guid buyerId)
    {
        var purchased = _context.PromotionCreditPurchases
            .Where(purchase => purchase.BuyerId == buyerId)
            .Select(purchase => (int?)purchase.CreditsPurchased)
            .Sum() ?? 0;
        var spent = _context.ProductPromotions
            .Count(promotion => promotion.Product.SellerId == buyerId);
        return purchased - spent;
    }

    private (int Credits, decimal Amount) GetPackage(PromotionPackage package) => package switch
    {
        PromotionPackage.Basic => ((int)PromotionPackage.Basic, _options.BasicPackagePrice),
        PromotionPackage.Standard => ((int)PromotionPackage.Standard, _options.StandardPackagePrice),
        PromotionPackage.Premium => ((int)PromotionPackage.Premium, _options.PremiumPackagePrice),
        _ => throw new ArgumentOutOfRangeException(nameof(package))
    };

    private void RequireActiveStudent(Guid userId)
    {
        var user = _context.ApplicationUsers.Find(userId);
        if (user?.Role != UserRole.Student || user.IsBlocked)
            throw new UnauthorizedAccessException("An active student account is required.");
    }
}
