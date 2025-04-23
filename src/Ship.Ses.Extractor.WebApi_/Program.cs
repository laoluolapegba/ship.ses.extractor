namespace Ship.Ses.Extractor.WebApi
{
    using Microsoft.AspNetCore.Builder;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Ship.Ses.Extractor.Application.Interfaces;
    using Ship.Ses.Extractor.Application.Services;
    using Ship.Ses.Extractor.Infrastructure;
    using Serilog;
    using System;
    using Ship.Ses.Extractor.Application.Services.DataMapping;
    using Ship.Ses.Extractor.Domain.Repositories.DataMapping;
    using Ship.Ses.Extractor.Infrastructure.Installers;
    using Microsoft.AspNetCore.Server.Kestrel.Core;
    using static Org.BouncyCastle.Math.EC.ECCurve;

    public class Program
    {
        public static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateLogger();

            try
            {
                Log.Information("Starting Ship.Ses.Extractor API");
                var builder = WebApplication.CreateBuilder(args);

                // Add Serilog
                builder.Host.UseSerilog((context, services, configuration) => configuration
                    .ReadFrom.Configuration(context.Configuration)
                    .ReadFrom.Services(services)
                    .Enrich.FromLogContext()
                    .WriteTo.Console());

                // Add services to the container
                builder.Services.AddControllers();

                // Add OpenAPI/Swagger
                builder.Services.AddEndpointsApiExplorer();
                builder.Services.AddSwaggerGen(c =>
                {
                    c.SwaggerDoc("v1", new() { Title = "Ship.Ses.Extractor API", Version = "v1" });
                });


                //configue kestrel
                builder.Services.Configure<KestrelServerOptions>(
                            builder.Configuration.GetSection("Kestrel"));

                // Add CORS
                //builder.Services.AddCors(options =>
                //{
                //    options.AddPolicy("AllowSpecificOrigin",
                //        builder =>
                //        {
                //            builder.WithOrigins("https://localhost:5001")
                //                .AllowAnyHeader()
                //                .AllowAnyMethod();
                //        });
                //});

                // Add application services
                builder.Services.AddScoped<IEmrDatabaseService, EmrDatabaseService>();
                builder.Services.AddScoped<IFhirResourceService, FhirResourceService>();
                builder.Services.AddScoped<IMappingService, MappingService>();

                // Add infrastructure services
                builder.Services.AddInfrastructure(builder.Configuration);

               
                var app = builder.Build();

                // Configure the HTTP request pipeline
                if (app.Environment.IsDevelopment())
                {
                    app.UseSwagger();
                    app.UseSwaggerUI();
                    app.UseDeveloperExceptionPage();
                }
                else
                {
                    app.UseExceptionHandler("/error");
                    app.UseHsts();
                }

                app.UseHttpsRedirection();
                app.UseCors("AllowSpecificOrigin");
                app.UseRouting();
                app.UseAuthorization();
                app.MapControllers();

                app.Run();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Application terminated unexpectedly");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }

}
