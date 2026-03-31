using MediatR;
using Microsoft.EntityFrameworkCore;
using WebDemo.Application.Common.Interfaces;

namespace WebDemo.Application.Features.Products.Queries;

public record GetProductsQuery : IRequest<List<ProductDto>>;

public class GetProductsQueryHandler 
    : IRequestHandler<GetProductsQuery, List<ProductDto>>
{
    private readonly IApplicationDbContext _context;

    public GetProductsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<ProductDto>> Handle(
        GetProductsQuery request,
        CancellationToken cancellationToken)
    {
        return await _context.Products
            .AsNoTracking()
            .Select(p => new ProductDto
            {
                Id = p.Id,
                Name = p.Name,
                Price = p.Price
            })
            .ToListAsync(cancellationToken);
    }
}