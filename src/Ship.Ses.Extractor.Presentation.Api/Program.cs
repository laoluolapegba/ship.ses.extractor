using Asp.Versioning.ApiExplorer;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.Options;
using Serilog;
using Swashbuckle.AspNetCore.SwaggerGen;
using Ship.Ses.Extractor.Presentation.Api.Helpers;
using Ship.Ses.Extractor.Application.Interfaces;
using Ship.Ses.Extractor.Application.Services.DataMapping;
using Ship.Ses.Extractor.Domain.Repositories.DataMapping;
using Ship.Ses.Extractor.Infrastructure.Installers;
using Ship.Ses.Extractor.Infrastructure.Persistance.Repositories;
using Ship.Ses.Extractor.Domain.Entities.DataMapping;
using Ship.Ses.Extractor.Infrastructure.Services;

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Configure Serilog
    Log.Logger = new LoggerConfiguration()
        .ReadFrom.Configuration(builder.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day)
        .CreateLogger();

    builder.Host.UseSerilog();

    // Read CORS settings from configuration
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowBlazorClient", policy =>
        {
            if (builder.Environment.IsDevelopment())
            {
                policy.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            }
            else
            {
                var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                                  ?? Array.Empty<string>();

                policy.WithOrigins(corsOrigins)
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .AllowCredentials();
            }
        });
    });

    // Add services to the container
    builder.Services.AddControllers();

    // Configure API versioning
    builder.Services.AddApiVersioning(options =>
    {
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.ReportApiVersions = true;
    })
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });

    // Configure Swagger
    builder.Services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();
    builder.Services.AddSwaggerGen();

    // Add application services
    builder.Services.AddScoped<IEmrDatabaseService, EmrDatabaseService>();
    builder.Services.AddScoped<Func<EmrConnection, IEmrDatabaseReader>>(serviceProvider => connection =>
    {
        return new EmrDatabaseReader(connection);
    });
    builder.Services.AddScoped<IFhirResourceService, FhirResourceService>();
    builder.Services.AddScoped<IMappingService, MappingService>();
    builder.Services.AddScoped<IMappingRepository, MappingRepository>();
    builder.Services.AddScoped<IEmrConnectionRepository, EmrConnectionRepository>();
    // Add infrastructure services
    builder.Services.AddInfrastructure(builder.Configuration);
    var app = builder.Build();

    // Log application startup
    app.Logger.LogInformation("🚀 Application starting up");

    // Configure middleware
    if (app.Environment.IsDevelopment())
    {
        app.Logger.LogInformation("🛠️ Development environment detected");
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            var provider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();
            foreach (var description in provider.ApiVersionDescriptions)
            {
                options.SwaggerEndpoint($"/swagger/{description.GroupName}/swagger.json", description.GroupName.ToUpperInvariant());
                options.RoutePrefix = string.Empty; // Serve Swagger UI at application root
            }
        });
    }
    else
    {
        app.Logger.LogInformation("🏭 Production environment detected");
        app.UseExceptionHandler("/Error");
        app.UseHsts();
    }

    app.UseSerilogRequestLogging();

    app.UseHttpsRedirection();
    app.UseAuthorization();
    app.MapControllers();

    // Log application ready
    app.Logger.LogInformation("✅ Application started and ready to accept requests");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "❌ Application failed to start due to an exception");
}
finally
{
    Log.CloseAndFlush();
}
