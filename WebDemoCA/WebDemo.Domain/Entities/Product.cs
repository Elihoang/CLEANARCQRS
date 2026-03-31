using WebDemo.Domain.Events;
using WebDemo.Domain.Common;
using WebDemo.Domain.Exceptions;

namespace WebDemo.Domain.Entities;

public class Product : BaseEntity
{
    public string Name { get; private set; } = null!;
    public decimal Price { get; private set; }

    private Product() { } // EF

    public Product(string name, decimal price)
    {
        Validate(name, price);

        Name = name;
        Price = price;

        AddDomainEvent(new ProductCreatedEvent(this));
    }

    public static Product Create(string name, decimal price)
    {
        return new Product(name, price);
    }

    public void Update(string name, decimal price)
    {
        Validate(name, price);

        Name = name;
        Price = price;

        // Optional:
        // AddDomainEvent(new ProductUpdatedEvent(this));
    }

    private static void Validate(string name, decimal price)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Name is required");

        if (price <= 0)
            throw new DomainException("Price must be greater than 0");
    }
}