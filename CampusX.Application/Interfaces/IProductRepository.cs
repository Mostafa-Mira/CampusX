using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CampusX.Application.DTOs;
using CampusX.Domain.Entities;

namespace CampusX.Application.Interfaces;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id);
    Task AddAsync(Product product);
    Task UpdateAsync(Product product);
    Task<PagedResult<ProductSummaryDto>> SearchAsync(ProductSearchFilterDto filter);
}
