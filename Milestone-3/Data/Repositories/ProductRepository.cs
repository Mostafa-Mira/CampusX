using System.Linq.Expressions;
using CampusX.Data.DbContexts;
using CampusX.Models;
using Microsoft.EntityFrameworkCore;

namespace CampusX.Data.Repositories;

public sealed class ProductRepository(CampusXDbContext context)
{
    public List<Product> ReadCatalog(IReadOnlyList<Expression<Func<Product, bool>>> filters,
        Func<IQueryable<Product>, IOrderedQueryable<Product>> order)
    {
        IQueryable<Product> query = context.Products.AsNoTracking()
            .Include(product => product.Category)
            .Include(product => product.Seller);

        foreach (var filter in filters)
            query = query.Where(filter);

        return order(query).ToList();
    }

    public Product? ReadDetails(Expression<Func<Product, bool>> matches) =>
        context.Products.AsNoTracking()
            .Include(product => product.Category)
            .Include(product => product.Seller)
            .FirstOrDefault(matches);

    public List<Product> ReadSellerListings(Expression<Func<Product, bool>> matches,
        Expression<Func<Product, DateTime>> newestFirst) =>
        context.Products.AsNoTracking()
            .Include(product => product.Category)
            .Where(matches)
            .OrderByDescending(newestFirst)
            .ToList();

    public Product? Find(Guid id) => context.Products.Find(id);

    public async Task<Product?> FindAsync(Guid id) => await context.Products.FindAsync(id);

    public Task<Product?> FindWithCategoryAsync(Guid id) =>
        context.Products.Include(product => product.Category)
            .FirstOrDefaultAsync(product => product.Id == id);

    public void Add(Product product) => context.Products.Add(product);

    public void Remove(Product product) => context.Products.Remove(product);

    public void SaveChanges() => context.SaveChanges();

    public Task SaveChangesAsync() => context.SaveChangesAsync();
}
