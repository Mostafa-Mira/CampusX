using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CampusX.Application.DTOs;

public record CreateInquiryDto(
    Guid ProductId,
    string Message
);

public record InquiryResponseDto(
    Guid Id,
    Guid ProductId,
    string ProductTitle,
    Guid BuyerId,
    string BuyerName,
    Guid SellerId,
    string SellerName,
    string Message,
    DateTime CreatedAt
);
