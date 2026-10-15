using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CampusX.Application.DTOs;
using CampusX.Application.Interfaces;
using CampusX.Domain.Entities;

namespace CampusX.Application.Services;

public class ProductService : IProductService
{
    private readonly IProductRepository _productRepository;

    public ProductService(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<ProductDetailsDto> CreateAsync(Guid sellerId, CreateProductDto dto)
    {
        var product = new Product(dto.Title, dto.Description, dto.Price, dto.ImageUrl, sellerId, dto.CategoryId);
        await _productRepository.AddAsync(product);

        return new ProductDetailsDto(
            product.Id,
            product.Title,
            product.Description,
            product.Price,
            product.ImageUrl,
            product.Status,
            product.CreatedAt,
            product.SellerId,
            "",
            "",
            product.CategoryId,
            ""
        );
    }

    public async Task UpdateAsync(Guid productId, Guid actorId, UpdateProductDto dto)
    {
        var product = await _productRepository.GetByIdAsync(productId);
        if (product == null || product.SellerId != actorId)
            throw new UnauthorizedAccessException("Unauthorized or product not found.");

        product.UpdateDetails(dto.Title, dto.Description, dto.Price, dto.ImageUrl, dto.CategoryId);
        await _productRepository.UpdateAsync(product);
    }

    public async Task<ProductDetailsDto?> GetDetailsAsync(Guid productId)
    {
        var product = await _productRepository.GetByIdAsync(productId);
        if (product == null) return null;

        return new ProductDetailsDto(
            product.Id,
            product.Title,
            product.Description,
            product.Price,
            product.ImageUrl,
            product.Status,
            product.CreatedAt,
            product.SellerId,
            product.Seller?.FullName ?? "",
            product.Seller?.CollegeName ?? "",
            product.CategoryId,
            product.Category?.Name ?? ""
        );
    }

    public async Task<PagedResult<ProductSummaryDto>> SearchAsync(ProductSearchFilterDto filter)
    {
        return await _productRepository.SearchAsync(filter);
    }
}
