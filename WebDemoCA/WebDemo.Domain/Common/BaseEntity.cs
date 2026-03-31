namespace WebDemo.Domain.Common;

public abstract class BaseEntity
{
    public int Id { get; set; }

    private readonly List<object> _domainEvents = new();

    public IReadOnlyCollection<object> DomainEvents => _domainEvents;

    public void AddDomainEvent(object eventItem)
    {
        _domainEvents.Add(eventItem);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}