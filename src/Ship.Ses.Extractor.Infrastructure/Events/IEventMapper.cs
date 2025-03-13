using Ship.Ses.Extractor.Application.Shared;
using Ship.Ses.Extractor.Domain;

namespace Ship.Ses.Extractor.Infrastructure.Events
{
    public interface IEventMapper
    {
        IntegrationEvent Map(IDomainEvent domainEvent);
    }
}
