using CampusX.Application.Interfaces;
using CampusX.Domain.Entities;
using CampusX.Domain.Enums;
using CampusX.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



using CampusX.Application.DTOs;
using CampusX.Application.Interfaces;
using CampusX.Domain.Entities;
using CampusX.Domain.Enums;
using CampusX.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CampusX.Infrastructure.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly CampusXDbContext _context;

    public ProductRepository(CampusXDbContext context)
    {
        _context = context;
    }

    public async Task<Product?> GetByIdAsync(Guid id)
    {
        return await _context.Products
          .Include(p => p.Seller)
          .Include(p => p.Category)
          .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
    }

    public async Task AddAsync(Product product)
    {
        await _context.Products.AddAsync(product);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Product product)
    {
        _context.Products.Update(product);
        await _context.SaveChangesAsync();
    }

    public async Task<PagedResult<ProductSummaryDto>> SearchAsync(ProductSearchFilterDto filter)
    {
        var query = _context.Products
          .AsNoTracking()
          .Where(p => !p.IsDeleted);

        // Filter by Search Term
        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var term = filter.SearchTerm.Trim().ToLower();
            query = query.Where(p => p.Title.ToLower().Contains(term) ||
                        p.Description.ToLower().Contains(term));
        }

        // Filter by Category
        if (filter.CategoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == filter.CategoryId.Value);
        }

        // Filter by Price
        if (filter.MinPrice.HasValue)
        {
            query = query.Where(p => p.Price >= filter.MinPrice.Value);
        }

        if (filter.MaxPrice.HasValue)
        {
            query = query.Where(p => p.Price <= filter.MaxPrice.Value);
        }

        // Filter by Status
        if (filter.Status.HasValue)
        {
            query = query.Where(p => p.Status == filter.Status.Value);
        }
        else
        {
            query = query.Where(p => p.Status == ProductStatus.Available);
        }

        var totalCount = await query.CountAsync();

        var items = await query
          .OrderByDescending(p => p.CreatedAt)
          .Skip((filter.Page - 1) * filter.PageSize)
          .Take(filter.PageSize)
          .Select(p => new ProductSummaryDto(
            p.Id,
            p.Title,
            p.Price,
            p.ImageUrl,
            p.Status
          ))
          .ToListAsync();

        return new PagedResult<ProductSummaryDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }
}

