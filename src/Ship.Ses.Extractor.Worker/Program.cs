using Hl7.Fhir.Model.CdsHooks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MySql.Data.MySqlClient;
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

    builder.Services.AddOptions<AppSettings>()
    .Bind(builder.Configuration.GetSection("AppSettings"))
    .Validate(o => !string.IsNullOrWhiteSpace(o.OriginDbType), "OriginDbType required")
    .Validate(o => !string.IsNullOrWhiteSpace(o.OriginDb.ConnectionString), "OriginDb.ConnectionString required")
    .ValidateOnStart();

    static void UseProviderWithSchema(DbContextOptionsBuilder opts, AppSettings app)
    {
        var kind = app.OriginDbType.Trim().ToLowerInvariant();
        var schema = string.IsNullOrWhiteSpace(app.Schema) ? null : app.Schema;

        switch (kind)
        {
            case "postgres":
            case "postgresql":
                {
                    var cs = schema is null
                        ? app.OriginDb.ConnectionString
                        : $"{app.OriginDb.ConnectionString};Search Path={schema}";
                    opts.UseNpgsql(cs);
                    break;
                }
            case "sqlserver":
                opts.UseSqlServer(app.OriginDb.ConnectionString);
                break;

            case "mysql":
                opts.UseMySQL(app.OriginDb.ConnectionString);
                break;

            default:
                throw new InvalidOperationException($"Unsupported OriginDbType: {app.OriginDbType}");
        }
    }
    // Register DbContext
    builder.Services.AddDbContext<ExtractorDbContext>((sp, opts) =>
    {
        var app = sp.GetRequiredService<IOptions<AppSettings>>().Value;
        UseProviderWithSchema(opts, app);
    });
    Log.Information("Origin DB provider: {Provider} | DB: {Database}",
        appSettings.OriginDbType, new MySqlConnectionStringBuilder(appSettings.OriginDb.ConnectionString).Database);

    //builder.Services.AddDbContext<ExtractorDbContext>(opts =>
    //{
    //    var app = builder.Configuration.GetSection("AppSettings").Get<AppSettings>();
    //    var cs = app.OriginDb.ConnectionString;
    //    var kind = app.OriginDbType?.Trim().ToLowerInvariant();

    //    switch (kind)
    //    {
    //        case "mysql":
    //            opts.UseMySQL(cs);            
    //            break;
    //        case "postgres":
    //        case "postgresql":
    //            opts.UseNpgsql(cs);          
    //            break;
    //        case "sqlserver":
    //            opts.UseSqlServer(cs);
    //            break;
    //        default:
    //            throw new InvalidOperationException($"Unsupported OriginDbType: '{app.OriginDbType}'. Use 'MySql' | 'Postgres' | 'SqlServer'.");
    //    }
    //    Log.Information("Origin DB provider: {Provider} | DB: {Database}", app.OriginDbType, new MySqlConnectionStringBuilder(cs).Database);

    //});




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