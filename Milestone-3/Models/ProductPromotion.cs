namespace CampusX.Models;

public class ProductPromotion
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProductId { get; set; }
    public DateTime ActivatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }

    public Product Product { get; set; } = null!;
}
