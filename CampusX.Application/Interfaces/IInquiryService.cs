using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CampusX.Application.DTOs;

namespace CampusX.Application.Interfaces;

public interface IInquiryService
{
    Task CreateAsync(Guid buyerId, CreateInquiryDto dto);
    Task<IEnumerable<InquiryResponseDto>> GetForUserAsync(Guid userId);
}
