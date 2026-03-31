using MediatR;
using WebDemo.Application.Common.Interfaces;
using WebDemo.Domain.Entities;

namespace WebDemo.Application.Features.Products.Commands.CreateProduct;

public record CreateProductCommand : IRequest<int>
{
    public string Name { get; init; } = null!;
    public decimal Price { get; init; }
}

public class CreateProductCommandHandler 
    : IRequestHandler<CreateProductCommand, int>
{
    private readonly IApplicationDbContext _context;

    public CreateProductCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> Handle(
        CreateProductCommand request,
        CancellationToken cancellationToken)
    {
        var entity = Product.Create(request.Name, request.Price);

        _context.Products.Add(entity);

        await _context.SaveChangesAsync(cancellationToken);

        return entity.Id;
    }
}