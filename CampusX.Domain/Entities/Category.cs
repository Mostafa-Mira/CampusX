using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CampusX.Domain.Entities;

public class Category
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string? IconName { get; private set; }

    // Navigation Property
    public ICollection<Product> Products { get; private set; } = new List<Product>();

    private Category() { }

    public Category(string name, string? description = null, string? iconName = null)
    {
        Id = Guid.NewGuid();
        Name = name;
        Description = description;
        IconName = iconName;
    }

    public void Rename(string newName, string? description = null, string? iconName = null)
    {
        Name = newName;
        Description = description;
        IconName = iconName;
    }
}
