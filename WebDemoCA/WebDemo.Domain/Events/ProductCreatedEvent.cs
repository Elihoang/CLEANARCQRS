using WebDemo.Domain.Common;
using WebDemo.Domain.Entities;

namespace WebDemo.Domain.Events;

public class ProductCreatedEvent : BaseEvent
{
    public Product Product { get; }

    public ProductCreatedEvent(Product product)
    {
        Product = product;
    }
}