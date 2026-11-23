using CampusX.Models;

namespace CampusX.Data.Repositories;

public interface ICategoryRepository
{
    List<Category> ListByName();
    Category? Find(Guid id);
    Task<Category?> FindByNameAsync(string name);
    bool Exists(Guid id);
    void Add(Category category);
}
