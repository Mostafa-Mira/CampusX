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

public class ReportRepository : IReportRepository
{
    private readonly CampusXDbContext _context;

    public ReportRepository(CampusXDbContext context)
    {
        _context = context;
    }

    public async Task<Report?> GetByIdAsync(Guid id)
    {
        return await _context.Reports
            .Include(r => r.Reporter)
            .Include(r => r.TargetProduct)
            .Include(r => r.TargetUser)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task AddAsync(Report report)
    {
        await _context.Reports.AddAsync(report);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Report report)
    {
        _context.Reports.Update(report);
        await _context.SaveChangesAsync();
    }
}
