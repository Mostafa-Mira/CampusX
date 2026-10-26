using CampusX.Data.DbContexts;
using CampusX.Enums;

namespace CampusX.Models;

// Represents the stored inquiry already present in the pulled CampusX model.
public class ContactInquiryBL
{
    private readonly CampusXDbContext _context;

    public ContactInquiryBL(CampusXDbContext context) => _context = context;

    public ContactInquiry Send(Guid buyerId, Guid productId, string message)
    {
        var buyer = _context.ApplicationUsers.Find(buyerId);
        if (buyer?.Role != UserRole.Student || buyer.IsBlocked)
            throw new UnauthorizedAccessException("An active student account is required.");

        var product = _context.Products.Find(productId)
            ?? throw new InvalidOperationException("Product not found.");
        if (product.IsDeleted || buyerId == product.SellerId)
            throw new InvalidOperationException("This seller cannot be contacted for this product.");
        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("A message is required.", nameof(message));

        var inquiry = new ContactInquiry
        {
            BuyerId = buyerId,
            ProductId = productId,
            Message = message.Trim()
        };
        _context.ContactInquiries.Add(inquiry);
        _context.SaveChanges();
        return inquiry;
    }
}
