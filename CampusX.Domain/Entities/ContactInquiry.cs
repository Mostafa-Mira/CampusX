using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CampusX.Domain.Entities;

public class ContactInquiry
{
    public Guid Id { get; private set; }
    public Guid ProductId { get; private set; }
    public Guid BuyerId { get; private set; }
    public Guid SellerId { get; private set; }
    public string Message { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }

    // Navigation Properties
    public Product Product { get; private set; } = null!;
    public ApplicationUser Buyer { get; private set; } = null!;
    public ApplicationUser Seller { get; private set; } = null!;

    private ContactInquiry() { }

    public ContactInquiry(Guid productId, Guid buyerId, Guid sellerId, string message)
    {
        Id = Guid.NewGuid();
        ProductId = productId;
        BuyerId = buyerId;
        SellerId = sellerId;
        Message = message;
        CreatedAt = DateTime.UtcNow;
    }

    public bool ValidateParticipants()
    {
        return BuyerId != SellerId;
    }
}
