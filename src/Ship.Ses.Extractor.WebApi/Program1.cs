using Ship.Ses.Extractor.Infrastructure.Installers;
using Ship.Ses.Extractor.Infrastructure.Persistance.MsSql;
using Scalar.AspNetCore;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);


// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.InstallSwagger();
//builder.InstallApplicationSettings();








builder.InstallDependencyInjectionRegistrations();
builder.Services.AddOpenApi();



var app = builder.Build();


using (var scope = app.Services.CreateScope())
{
    var appDbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    //EntityFrameworkInstaller.SeedDatabase(appDbContext);
};


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.UseExceptionHandler();

app.Run();
