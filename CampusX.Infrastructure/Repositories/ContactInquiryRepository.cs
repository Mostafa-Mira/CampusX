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

public class ContactInquiryRepository : IContactInquiryRepository
{
    private readonly CampusXDbContext _context;

    public ContactInquiryRepository(CampusXDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(ContactInquiry inquiry)
    {
        await _context.ContactInquiries.AddAsync(inquiry);
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<ContactInquiry>> GetForUserAsync(Guid userId)
    {
        return await _context.ContactInquiries
            .Include(i => i.Product)
            .Include(i => i.Buyer)
            .Include(i => i.Seller)
            .Where(i => i.BuyerId == userId || i.SellerId == userId)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync();
    }
}
