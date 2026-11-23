using CampusX.Data.DbContexts;
using CampusX.Models;
using Microsoft.EntityFrameworkCore;

namespace CampusX.Data.Repositories;

public sealed class CategoryRepository(CampusXDbContext context) : ICategoryRepository
{
    public List<Category> ListByName() =>
        context.Categories.OrderBy(category => category.Name).ToList();

    public Category? Find(Guid id) => context.Categories.Find(id);

    public Task<Category?> FindByNameAsync(string name) =>
        context.Categories.FirstOrDefaultAsync(category => category.Name == name);

    public bool Exists(Guid id) => context.Categories.Any(category => category.Id == id);

    public void Add(Category category) => context.Categories.Add(category);
}
