using System;
using System.Collections.Generic;
using System.Text;

namespace CampusX.Models
{
    public class ContactInquiry
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ProductId { get; set; }
        public Guid BuyerId { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public Product Product { get; set; } = null!;
        public ApplicationUser Buyer { get; set; } = null!;

        public bool ValidateParticipants(Guid sellerId)
        {
            return BuyerId != sellerId;
        }
    }
}
