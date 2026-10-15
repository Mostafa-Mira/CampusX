using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CampusX.Domain.Entities;

namespace CampusX.Application.Interfaces;

public interface IContactInquiryRepository
{
    Task AddAsync(ContactInquiry inquiry);
    Task<IEnumerable<ContactInquiry>> GetForUserAsync(Guid userId);
}
