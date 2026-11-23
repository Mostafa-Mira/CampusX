using System.Linq.Expressions;
using CampusX.Models;

namespace CampusX.Data.Repositories;

public interface IProductRepository
{
    List<Product> ReadCatalog(IReadOnlyList<Expression<Func<Product, bool>>> filters,
        Func<IQueryable<Product>, IOrderedQueryable<Product>> order);

    Product? ReadDetails(Expression<Func<Product, bool>> matches);

    List<Product> ReadSellerListings(Expression<Func<Product, bool>> matches,
        Expression<Func<Product, DateTime>> newestFirst);

    Product? Find(Guid id);
    Task<Product?> FindAsync(Guid id);
    Task<Product?> FindWithCategoryAsync(Guid id);

    void Add(Product product);
    void Remove(Product product);
    void SaveChanges();
    Task SaveChangesAsync();
}
