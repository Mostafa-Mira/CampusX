using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CampusX.Application.DTOs;

namespace CampusX.Application.Interfaces;

public interface IProductService
{
    Task<ProductDetailsDto> CreateAsync(Guid sellerId, CreateProductDto dto);
    Task UpdateAsync(Guid productId, Guid actorId, UpdateProductDto dto);
    Task<ProductDetailsDto?> GetDetailsAsync(Guid productId);
    Task<PagedResult<ProductSummaryDto>> SearchAsync(ProductSearchFilterDto filter);
}