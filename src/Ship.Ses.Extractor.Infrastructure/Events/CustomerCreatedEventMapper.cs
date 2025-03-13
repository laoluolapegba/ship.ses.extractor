using Ship.Ses.Extractor.Application.Customer.CreateCustomer;
using Ship.Ses.Extractor.Application.Shared;
using Ship.Ses.Extractor.Domain;
using Ship.Ses.Extractor.Domain.Customers.DomainEvents;
using Newtonsoft.Json;

namespace Ship.Ses.Extractor.Infrastructure.Events
{
    public class CustomerCreatedEventMapper : IEventMapper
    {
        private readonly IDateTimeProvider _dateTimeProvider;

        public CustomerCreatedEventMapper(IDateTimeProvider dateTimeProvider)
        {
            _dateTimeProvider = dateTimeProvider;
        }
        public IntegrationEvent Map(IDomainEvent domainEvent)
        {

            var integrationEvent = new IntegrationEvent(
                Guid.NewGuid(),
                _dateTimeProvider.UtcNow,
                typeof(CustomerCreatedIntegrationEvent).FullName,
                typeof(CustomerCreatedIntegrationEvent).Assembly.GetName().Name,
                JsonConvert.SerializeObject(domainEvent as CustomerCreatedDomainEvent, new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.None }));

            return integrationEvent;

        }
    }
}
