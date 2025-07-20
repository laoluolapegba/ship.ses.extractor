using Hl7.Fhir.Model.CdsHooks;
using Ship.Ses.Extractor.Application.Services.Extractors;
using Ship.Ses.Extractor.Application.Services.Transformers;
using Ship.Ses.Extractor.Application.Services.Validators;
using Ship.Ses.Extractor.Domain.Repositories.Transformer;
using Ship.Ses.Extractor.Domain.Repositories.Validator;
using Ship.Ses.Extractor.Infrastructure.Extensions;
using Ship.Ses.Extractor.Infrastructure.Validator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Ship.Ses.Extractor.Worker.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddExtractorDependencies(this IServiceCollection services, IConfiguration config)
        {
            services.AddInfrastructureServices(config);

            // Application logic

            // Register the FhirValidator as the implementation for IFhirResourceValidator
            services.AddScoped<IFhirResourceValidator, FirelyResourceValidator>();

            // Register the FhirProcessingService (it depends on IFhirResourceValidator)
            services.AddScoped<FhirValidatorService>();

            //services.AddSingleton<IResourceTransformer<JsonObject>, PatientTransformer>();
            //services.AddSingleton<IResourceTransformer<JsonObject>, EncounterTransformer>();
            services.AddSingleton<EncounterTransformer>();
            services.AddSingleton<PatientTransformer>();
            services.AddSingleton<ObservationTransformer>();
            services.AddSingleton<ConditionTransformer>();

            // Orchestrator
            services.AddScoped<PatientResourceExtractor>();
            services.AddScoped<EncounterResourceExtractor>();
            services.AddScoped<ObservationResourceExtractor>();
            services.AddScoped<ConditionResourceExtractor>();
            return services;
        }
    }
}
