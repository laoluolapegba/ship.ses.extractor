using Ship.Ses.Extractor.Application.Shared;
using Ship.Ses.Extractor.Domain;
using Ship.Ses.Extractor.Domain.Customers;
using Ship.Ses.Extractor.Domain.Customers.DomainEvents;
using Ship.Ses.Extractor.Domain.Orders;
using Ship.Ses.Extractor.Infrastructure.Authentication;
using Ship.Ses.Extractor.Infrastructure.BackgroundTasks;
using Ship.Ses.Extractor.Infrastructure.Events;
using Ship.Ses.Extractor.Infrastructure.Exceptions;
using Ship.Ses.Extractor.Infrastructure.Persistance.Configuration.Domain.Customers;
using Ship.Ses.Extractor.Infrastructure.Persistance.Configuration.Domain.Orders;
using Ship.Ses.Extractor.Infrastructure.ReadServices;
using Ship.Ses.Extractor.Infrastructure.Shared;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Ship.Ses.Extractor.Infrastructure.Installers
{
    public static class DependencyInjectionInstaller
    {
        public static void InstallDependencyInjectionRegistrations(this WebApplicationBuilder builder)
        {
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
            builder.Services.AddScoped<IOrderRepository, OrderRepository>();
            builder.Services.AddTransient<IDateTimeProvider, DateTimeProvider>();
            builder.Services.AddTransient<IDomainEventDispatcher, DomainEventDispatcher>();
            builder.Services.AddHostedService<DomainEventsProcessor>();
            builder.Services.AddHostedService<IntegrationEventsProcessor>();

            builder.Services.AddTransient<CustomerCreatedEventMapper>();
            builder.Services.AddSingleton<EventMapperFactory>(provider =>
            {
                var mappers = new Dictionary<Type, IEventMapper>
                {
                    { typeof(CustomerCreatedDomainEvent), provider.GetRequiredService<CustomerCreatedEventMapper>() },
                };

                return new EventMapperFactory(mappers);
            });
            builder.Services.AddValidatorsFromAssemblyContaining<IApplicationValidator>(ServiceLifetime.Transient);
            builder.Services.AddProblemDetails();
            builder.Services.AddExceptionHandler<CommandValidationExceptionHandler>();
            builder.Services.AddSingleton<ICacheService, CacheService>();
            builder.Services.AddScoped<IEmailService, EmailService>();
            builder.Services.AddScoped<IEmailTemplateFactory, EmailTemplateFactory>();
            builder.Services.AddScoped<OrderDomainService>();
            builder.Services.AddScoped<IOrderReadService, OrderReadService>();
            builder.Services.AddScoped<ICustomerReadService, CustomerReadService>();
            builder.Services.AddHttpClient<IAuthenticationService, KeycloakAuthenticationService>();

        }

    }
}
