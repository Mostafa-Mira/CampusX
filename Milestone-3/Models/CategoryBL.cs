using CampusX.Data.DbContexts;

namespace CampusX.Models;

public class CategoryBL
{
    private readonly CampusXDbContext _context;

    public CategoryBL(CampusXDbContext context) => _context = context;

    public List<Category> GetAll() =>
        _context.Categories.OrderBy(category => category.Name).ToList();

    public Category? GetById(Guid id) => _context.Categories.Find(id);
}
