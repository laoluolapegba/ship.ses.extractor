using Ship.Ses.Extractor.Domain.Repositories.Extractor;
using Ship.Ses.Extractor.Domain.Shared;
using Ship.Ses.Extractor.Infrastructure.Persistance.Contexts;
using Ship.Ses.Extractor.Worker;
using Microsoft.EntityFrameworkCore;
using Ship.Ses.Extractor.Infrastructure.Settings;

using Ship.Ses.Extractor.Worker.Extensions;
using Serilog; // Fix for CS1061: Ensure the correct namespace for EF Core is included.


Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(Host.CreateApplicationBuilder().Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();
try
{
    Log.Information("Starting Ship Extractor Worker...");

    var builder = Host.CreateApplicationBuilder(args);
    // Load strongly-typed settings
    // ✅ Bind app settings
    builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("AppSettings"));
    var appSettings = builder.Configuration.GetSection("AppSettings").Get<AppSettings>()
        ?? throw new Exception("AppSettings section not found.");

    builder.Services.Configure<AppSettings>(builder.Configuration.GetSection(nameof(AppSettings)));
    // Register DbContext
    builder.Services.AddDbContext<ExtractorDbContext>(options =>
    {
        options.UseMySQL(appSettings.OriginDb.ConnectionString);
    });

    // Register Infra + Application services via extension method
    builder.Services.AddExtractorDependencies(builder.Configuration);

    // Hosted Service (Runner)
    builder.Services.AddHostedService<PatientExtractorWorker>();

    builder.Build().Run();

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