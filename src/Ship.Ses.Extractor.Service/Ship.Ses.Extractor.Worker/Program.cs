using Ship.Ses.Extractor.Domain.Repositories.Extractor;
using Ship.Ses.Extractor.Domain.Shared;
using Ship.Ses.Extractor.Infrastructure.Persistance.Contexts;
using Ship.Ses.Extractor.Worker;
using Microsoft.EntityFrameworkCore;
using Ship.Ses.Extractor.Infrastructure.Settings;
using Ship.Ses.Extractor.Infrastructure.Persistance.Repositories;
using Ship.Ses.Extractor.Infrastructure.Shared;
using Ship.Ses.Extractor.Application.Services;
using Ship.Ses.Extractor.Domain.Repositories.Transformer;
using System.Text.Json.Nodes;
using Ship.Ses.Extractor.Infrastructure.Extraction;
using Ship.Ses.Extractor.Infrastructure.Configuration;
using Ship.Ses.Extractor.Application.Services.Extractors;
using Ship.Ses.Extractor.Application.Services.Transformers;
using Ship.Ses.Extractor.Worker.Extensions; // Fix for CS1061: Ensure the correct namespace for EF Core is included.

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
builder.Services.AddHostedService<PatientSyncWorker>();

builder.Build().Run();


//var builder = Host.CreateApplicationBuilder(args);
//var appSettings = builder.Configuration.GetSection(nameof(AppSettings)).Get<AppSettings>();
//if (appSettings != null)
//{

//    var msSqlSettings = appSettings.ShipServerSqlDb;
//    builder.Services.AddDbContext<ExtractorDbContext>(options =>
//    {
//        options.UseMySQL(msSqlSettings.ConnectionString);
//    });
//}
//else
//{
//    throw new Exception("AppSettings not found");
//}

//builder.Services.AddScoped<IDataSourceRepository, DataSourceRepository>();
//builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
//builder.Services.AddScoped<IDataExtractorService, EfSqlDataExtractorService>();
//builder.Services.AddSingleton<ITableMappingService, JsonTableMappingService>();
//builder.Services.AddSingleton<ITableMappingService, JsonTableMappingService>();

//builder.Services.AddSingleton<IResourceTransformer<JsonObject>, PatientTransformer>();

//builder.Services.AddSingleton<PatientResourceExtractor>();
//builder.Services.AddHostedService<PatientSyncWorker>();

//var host = builder.Build();
//host.Run();

