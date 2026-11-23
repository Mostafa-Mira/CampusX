using CampusX.Data.Repositories;

namespace CampusX.Models;

public class CategoryBL(ICategoryRepository categories)
{
    public List<Category> GetAll() => categories.ListByName();

    public Category? GetById(Guid id) => categories.Find(id);
}
