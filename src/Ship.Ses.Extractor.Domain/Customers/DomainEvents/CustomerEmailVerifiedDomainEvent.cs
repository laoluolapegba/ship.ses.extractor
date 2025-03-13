namespace Ship.Ses.Extractor.Domain.Customers.DomainEvents
{
    public sealed record CustomerEmailVerifiedDomainEvent(string NewEmailAddress) : IDomainEvent;
}
