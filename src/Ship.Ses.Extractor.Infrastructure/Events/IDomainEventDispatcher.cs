using Ship.Ses.Extractor.Domain;

namespace Ship.Ses.Extractor.Infrastructure.Events
{
    public interface IDomainEventDispatcher
    {
        Task Dispatch(IDomainEvent domainEvent);
    }
}
