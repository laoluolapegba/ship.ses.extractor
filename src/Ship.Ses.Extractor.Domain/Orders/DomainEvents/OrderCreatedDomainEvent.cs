namespace Ship.Ses.Extractor.Domain.Orders.DomainEvents
{
    public sealed record OrderCreatedDomainEvent(Guid OrderId, Guid CustomerId) : IDomainEvent;
}
