using MediatR;
using WebDemo.Domain.Events;

namespace WebDemo.Application.Features.Products.EventHandlers;

public class LogProductCreated 
    : INotificationHandler<ProductCreatedEvent>
{
    public Task Handle(ProductCreatedEvent notification, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Product created: {notification.Product.Name}");
        return Task.CompletedTask;
    }
}