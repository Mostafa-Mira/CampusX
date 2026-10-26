using CampusX.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace CampusX.Entities
{
    public class Category
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }

        // Navigation Property
        public ICollection<Product> Products { get; set; } = new List<Product>();

        public void Rename(string newName)
        {
            if (!string.IsNullOrWhiteSpace(newName))
                Name = newName;
        }
    }
}