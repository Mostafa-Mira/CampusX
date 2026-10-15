using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CampusX.Domain.Entities;

namespace CampusX.Application.Interfaces;

public interface IReportRepository
{
    Task<Report?> GetByIdAsync(Guid id);
    Task AddAsync(Report report);
    Task UpdateAsync(Report report);
}
