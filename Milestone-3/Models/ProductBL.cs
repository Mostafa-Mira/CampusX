using System.Linq.Expressions;
using CampusX.Data.DbContexts;
using CampusX.Data.Repositories;
using CampusX.Enums;
using CampusX.Services;

namespace CampusX.Models;

public class ProductBL
{
    private readonly CampusXDbContext _context;
    private readonly DevelopmentTesterAccess _testerAccess;
    private readonly IProductRepository _products;
    private readonly ICategoryRepository _categories;

    public ProductBL(CampusXDbContext context, DevelopmentTesterAccess testerAccess,
        IProductRepository products, ICategoryRepository categories)
    {
        _context = context;
        _testerAccess = testerAccess;
        _products = products;
        _categories = categories;
    }

    public List<Product> GetAvailable(string? name = null, Guid? categoryId = null,
        decimal? minPrice = null, decimal? maxPrice = null, string? sortBy = null)
    {
        if (minPrice < 0 || maxPrice < 0 || (minPrice.HasValue && maxPrice < minPrice))
            throw new ArgumentException("Invalid price range.");

        var now = DateTime.UtcNow;
        List<Expression<Func<Product, bool>>> filters =
            [product => !product.IsDeleted && product.Status == ProductStatus.Available];

        if (!string.IsNullOrWhiteSpace(name))
        {
            var searchTerm = name.Trim();
            filters.Add(product => product.Name.Contains(searchTerm));
        }
        if (categoryId.HasValue)
            filters.Add(product => product.CategoryId == categoryId.Value);
        if (minPrice.HasValue)
            filters.Add(product => product.Price >= minPrice.Value);
        if (maxPrice.HasValue)
            filters.Add(product => product.Price <= maxPrice.Value);

        return _products.ReadCatalog(filters, products =>
        {
            var featuredFirst = products.OrderByDescending(product =>
                product.Promotions.Any(promotion => promotion.ExpiresAt > now));
            Expression<Func<Product, DateTime?>> activePromotionDate = product => product.Promotions
                .Where(promotion => promotion.ExpiresAt > now)
                .Select(promotion => (DateTime?)promotion.ActivatedAt).Max();

            return sortBy switch
            {
                "price_asc" => featuredFirst.ThenBy(product => product.Price)
                    .ThenByDescending(activePromotionDate),
                "price_desc" => featuredFirst.ThenByDescending(product => product.Price)
                    .ThenByDescending(activePromotionDate),
                _ => featuredFirst.ThenByDescending(activePromotionDate)
                    .ThenByDescending(product => product.CreatedAt)
            };
        });
    }

    public Product? GetById(Guid id) =>
        _products.ReadDetails(product => product.Id == id && !product.IsDeleted);

    public List<Product> GetBySeller(Guid sellerId) =>
        _products.ReadSellerListings(product => product.SellerId == sellerId && !product.IsDeleted,
            product => product.CreatedAt);

    public async Task AddAsync(Guid studentId, Product product, string categoryName)
    {
        ApplicationUserBL.RequireEligibleRole(_context, studentId, UserRole.Student, _testerAccess);
        ValidateNameAndPrice(product.Name, product.Price);
        var category = await GetOrCreateCategoryAsync(categoryName);
        product.CategoryId = category.Id;
        product.SellerId = studentId;
        product.Status = ProductStatus.Available;
        product.IsDeleted = false;
        product.College = NormalizeCollege(product.College);
        _products.Add(product);
        await _products.SaveChangesAsync();
    }

    public async Task<Product> GetForEditAsync(Guid studentId, Guid productId) =>
        RequireEditableProduct(studentId, await _products.FindWithCategoryAsync(productId));

    public async Task EditAsync(Guid studentId, Guid productId, Product changes, string categoryName)
    {
        // Find returns the tracked listing loaded by the form action, preserving the concurrency tokens.
        var product = RequireEditableProduct(studentId, await _products.FindAsync(productId));
        ValidateNameAndPrice(changes.Name, changes.Price);
        var category = await GetOrCreateCategoryAsync(categoryName);
        product.UpdateDetails(changes.Name, changes.Description, changes.Price, changes.ImageUrl, category.Id);
        product.College = NormalizeCollege(changes.College);
        product.PhoneNumber = changes.PhoneNumber;
        await _products.SaveChangesAsync();
    }

    private async Task<Category> GetOrCreateCategoryAsync(string categoryName)
    {
        var name = categoryName.Trim();
        var category = await _categories.FindByNameAsync(name);
        if (category is not null)
            return category;

        // Both repositories share the request's DbContext. The product save also inserts this category.
        category = new Category { Name = name };
        _categories.Add(category);
        return category;
    }

    private static string NormalizeCollege(string? college) =>
        string.IsNullOrWhiteSpace(college) ? "General" : college;

    private static Product RequireEditableProduct(Guid studentId, Product? product)
    {
        if (product is null || product.IsDeleted)
            throw new KeyNotFoundException("Product not found.");
        RequireOwner(studentId, product);
        return product;
    }

    public Product Add(Guid studentId, Product product)
    {
        ApplicationUserBL.RequireEligibleRole(_context, studentId, UserRole.Student, _testerAccess);
        ValidateDetails(product.Name, product.Price, product.CategoryId);
        product.SellerId = studentId;
        product.Status = ProductStatus.Available;
        product.IsDeleted = false;
        _products.Add(product);
        _products.SaveChanges();
        return product;
    }

    public void Update(Guid studentId, Guid productId, string name, string description,
        decimal price, string imageUrl, Guid categoryId)
    {
        var product = GetOwnedProduct(studentId, productId);
        ValidateDetails(name, price, categoryId);
        product.UpdateDetails(name, description, price, imageUrl, categoryId);
        _products.SaveChanges();
    }

    public void MarkSold(Guid studentId, Guid productId)
    {
        var product = GetOwnedProduct(studentId, productId);
        product.ChangeStatus(ProductStatus.Sold);
        _products.SaveChanges();
    }

    public void Delete(Guid studentId, Guid productId)
    {
        var product = GetOwnedProduct(studentId, productId);
        _products.Remove(product);
        _products.SaveChanges();
    }

    public void RemoveByAdmin(Guid adminId, Guid productId)
    {
        ApplicationUserBL.RequireEligibleAdmin(_context, adminId);
        var product = GetExistingProduct(productId);
        product.IsDeleted = true;
        _products.SaveChanges();
    }

    private Product GetOwnedProduct(Guid studentId, Guid productId)
    {
        ApplicationUserBL.RequireEligibleRole(_context, studentId, UserRole.Student, _testerAccess);
        var product = GetExistingProduct(productId);
        RequireOwner(studentId, product);
        return product;
    }

    private static void RequireOwner(Guid studentId, Product product)
    {
        if (!product.IsOwnedBy(studentId))
            throw new UnauthorizedAccessException("Only the seller can manage this product.");
    }

    private Product GetExistingProduct(Guid productId)
    {
        var product = _products.Find(productId);
        return product is not null && !product.IsDeleted ? product
            : throw new InvalidOperationException("Product not found.");
    }

    private void ValidateDetails(string name, decimal price, Guid categoryId)
    {
        ValidateNameAndPrice(name, price);
        if (!_categories.Exists(categoryId))
            throw new ArgumentException("Category not found.", nameof(categoryId));
    }

    private static void ValidateNameAndPrice(string name, decimal price)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Product name is required.", nameof(name));
        if (price < 0)
            throw new ArgumentOutOfRangeException(nameof(price));
    }
}
