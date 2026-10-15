using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CampusX.Domain.Enums;

namespace CampusX.Application.DTOs;

public record CreateProductDto(
    string Title,
    string Description,
    decimal Price,
    string ImageUrl,
    Guid CategoryId
);

public record UpdateProductDto(
    string Title,
    string Description,
    decimal Price,
    string ImageUrl,
    Guid CategoryId
);

public record ProductSummaryDto(
    Guid Id,
    string Title,
    decimal Price,
    string ImageUrl,
    ProductStatus Status
);

public record ProductDetailsDto(
    Guid Id,
    string Title,
    string Description,
    decimal Price,
    string ImageUrl,
    ProductStatus Status,
    DateTime CreatedAt,
    Guid SellerId,
    string SellerName,
    string SellerCollege,
    Guid CategoryId,
    string CategoryName
);

public record ProductSearchFilterDto(
    string? SearchTerm = null,
    Guid? CategoryId = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    ProductStatus? Status = null,
    int Page = 1,
    int PageSize = 10
);
