using CampusX.Data.DbContexts;
using CampusX.Enums;
using Microsoft.EntityFrameworkCore;

namespace CampusX.Models;

public class ProductBL
{
    private readonly CampusXDbContext _context;

    public ProductBL(CampusXDbContext context) => _context = context;

    public List<Product> GetAvailable(string? name = null, Guid? categoryId = null,
        decimal? minPrice = null, decimal? maxPrice = null, string? sortBy = null)
    {
        if (minPrice < 0 || maxPrice < 0 || (minPrice.HasValue && maxPrice < minPrice))
            throw new ArgumentException("Invalid price range.");

        var products = _context.Products.AsNoTracking()
            .Include(product => product.Category)
            .Include(product => product.Seller)
            .Where(product => !product.IsDeleted && product.Status == ProductStatus.Available);

        if (!string.IsNullOrWhiteSpace(name))
            products = products.Where(product => product.Name.Contains(name.Trim()));
        if (categoryId.HasValue)
            products = products.Where(product => product.CategoryId == categoryId.Value);
        if (minPrice.HasValue)
            products = products.Where(product => product.Price >= minPrice.Value);
        if (maxPrice.HasValue)
            products = products.Where(product => product.Price <= maxPrice.Value);

       
        if (sortBy == "price_asc")
            products = products.OrderBy(product => product.Price);
        else if (sortBy == "price_desc")
            products = products.OrderByDescending(product => product.Price);
        else
            products = products.OrderByDescending(product => product.CreatedAt);

        return products.ToList();
    }

    public Product? GetById(Guid id) =>
        _context.Products.AsNoTracking()
            .Include(product => product.Category)
            .Include(product => product.Seller)
            .FirstOrDefault(product => product.Id == id && !product.IsDeleted);

    public List<Product> GetBySeller(Guid sellerId) =>
        _context.Products.AsNoTracking()
            .Where(product => product.SellerId == sellerId && !product.IsDeleted)
            .OrderByDescending(product => product.CreatedAt)
            .ToList();

    public Product Add(Guid studentId, Product product)
    {
        RequireActiveStudent(studentId);
        ValidateDetails(product.Name, product.Price, product.CategoryId);
        product.SellerId = studentId;
        product.Status = ProductStatus.Available;
        product.IsDeleted = false;
        _context.Products.Add(product);
        _context.SaveChanges();
        return product;
    }

    public void Update(Guid studentId, Guid productId, string name, string description,
        decimal price, string imageUrl, Guid categoryId)
    {
        var product = GetOwnedProduct(studentId, productId);
        ValidateDetails(name, price, categoryId);
        product.UpdateDetails(name, description, price, imageUrl, categoryId);
        _context.SaveChanges();
    }

    public void MarkSold(Guid studentId, Guid productId)
    {
        var product = GetOwnedProduct(studentId, productId);
        product.ChangeStatus(ProductStatus.Sold);
        _context.SaveChanges();
    }

    public void Delete(Guid studentId, Guid productId)
    {
        var product = GetOwnedProduct(studentId, productId);
        product.IsDeleted = true;
        _context.SaveChanges();
    }

    public void DeleteById(Guid pid)
    {
        var product = _context.Products.Find(pid);

        if (product is null || product.IsDeleted)
            throw new InvalidOperationException("Product was not found or has already been deleted.");

        product.IsDeleted = true;
        _context.SaveChanges();
    }

    public void RemoveByAdmin(Guid adminId, Guid productId)
    {
        if (_context.ApplicationUsers.Find(adminId)?.Role != UserRole.Admin)
            throw new UnauthorizedAccessException("Admin access is required.");

        var product = _context.Products.Find(productId)
            ?? throw new InvalidOperationException("Product not found.");
        product.IsDeleted = true;
        _context.SaveChanges();
    }

    private Product GetOwnedProduct(Guid studentId, Guid productId)
    {
        RequireActiveStudent(studentId);
        var product = _context.Products.Find(productId)
            ?? throw new InvalidOperationException("Product not found.");
        if (product.SellerId != studentId || product.IsDeleted)
            throw new UnauthorizedAccessException("Only the seller can manage this product.");
        return product;
    }

    private void RequireActiveStudent(Guid studentId)
    {
        var student = _context.ApplicationUsers.Find(studentId);
        if (student?.Role != UserRole.Student || student.IsBlocked)
            throw new UnauthorizedAccessException("An active student account is required.");
    }

    private void ValidateDetails(string name, decimal price, Guid categoryId)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Product name is required.", nameof(name));
        if (price < 0)
            throw new ArgumentOutOfRangeException(nameof(price));
        if (!_context.Categories.Any(category => category.Id == categoryId))
            throw new ArgumentException("Category not found.", nameof(categoryId));
    }
}
