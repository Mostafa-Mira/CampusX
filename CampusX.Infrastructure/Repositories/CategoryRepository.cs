using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CampusX.Application.Interfaces;
using CampusX.Domain.Entities;
using CampusX.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CampusX.Infrastructure.Repositories;

public class CategoryRepository : ICategoryRepository
{
    private readonly CampusXDbContext _context;

    public CategoryRepository(CampusXDbContext context)
    {
        _context = context;
    }

    public async Task<Category?> GetByIdAsync(Guid id)
    {
        return await _context.Categories
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<IEnumerable<Category>> GetAllAsync()
    {
        return await _context.Categories.ToListAsync();
    }

    public async Task AddAsync(Category category)
    {
        await _context.Categories.AddAsync(category);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Category category)
    {
        _context.Categories.Update(category);
        await _context.SaveChangesAsync();
    }
}
