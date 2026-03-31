using Microsoft.EntityFrameworkCore;
using WebDemo.Domain.Entities;

namespace WebDemo.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Product> Products { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}