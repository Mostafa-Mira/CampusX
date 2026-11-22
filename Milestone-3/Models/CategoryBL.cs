using CampusX.Data.Repositories;

namespace CampusX.Models;

public class CategoryBL(CategoryRepository categories)
{
    public List<Category> GetAll() => categories.ListByName();

    public Category? GetById(Guid id) => categories.Find(id);
}
