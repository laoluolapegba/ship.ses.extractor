using Hl7.Fhir.Model.CdsHooks;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Ship.Ses.Extractor.Application.Contracts;
using Ship.Ses.Extractor.Application.Services.Transformers;
using Ship.Ses.Extractor.Domain.Repositories.Extractor;
using Ship.Ses.Extractor.Domain.Repositories.Transformer;
using Ship.Ses.Extractor.Domain.Shared;
using Ship.Ses.Extractor.Infrastructure.Persistance.Contexts;
using Ship.Ses.Extractor.Infrastructure.Services;
using Ship.Ses.Extractor.Infrastructure.Settings;
using Ship.Ses.Extractor.Worker;
using Ship.Ses.Extractor.Worker.Extensions;
using static Ship.Ses.Extractor.Infrastructure.Services.FhirStagingIngestService;


Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(Host.CreateApplicationBuilder().Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();
try
{
    Log.Information("Starting Ship Extractor program...");

    var builder = Host.CreateApplicationBuilder(args);
    // Load strongly-typed settings
    //  Bind app settings
    builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("AppSettings"));
    var appSettings = builder.Configuration.GetSection("AppSettings").Get<AppSettings>()
        ?? throw new Exception("AppSettings section not found.");

    builder.Services.Configure<AppSettings>(builder.Configuration.GetSection(nameof(AppSettings)));
    var envDefaults = builder.Configuration.GetSection("EnvironmentDefaults").Get<EnvironmentDefaults>();
    builder.Services.AddSingleton(envDefaults);

    builder.Services.Configure<FhirStagingOptions>(builder.Configuration.GetSection("FhirStaging"));


    // Register DbContext
    builder.Services.AddDbContext<ExtractorDbContext>(options =>
    {
        options.UseMySQL(appSettings.OriginDb.ConnectionString);
    });

    // Register Infra + Application services via extension method
    builder.Services.AddExtractorDependencies(builder.Configuration);
    builder.Services.AddScoped<IFhirStagingIngestService, FhirStagingIngestService>();
    // Hosted Service (Runner)
    builder.Services.AddHostedService<PatientExtractorWorker>();
    //builder.Services.AddHostedService<EncounterExtractorWorker>();
    //builder.Services.AddHostedService<ObservationExtractorWorker>();

    TemplateBuilders.ConfigureDefaults(envDefaults);
    Log.Information($"environment defaults: {envDefaults.ManagingOrganization}");

    builder.Build().Run();
    TemplateBuilders.ConfigureDefaults(envDefaults);
    Log.Information("Ship Extractor Worker stopped cleanly.");
}
catch (Exception ex)
{
    Log.Fatal(ex, "Ship Extractor Worker terminated unexpectedly.");
}
finally
{
    Log.CloseAndFlush();
}