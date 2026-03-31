using FluentValidation;
namespace WebDemo.Application.Features.Products.Commands.CreateProduct;

public class CreateProductCommandValidator 
    : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(v => v.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(v => v.Price)
            .GreaterThan(0);
    }
}