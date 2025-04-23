using Microsoft.AspNetCore.Components.Web;
using Ship.Ses.Extractor.UI.BlazorWeb.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Toolbelt.Blazor.Extensions.DependencyInjection;
using Blazored.LocalStorage;
using Ship.Ses.Extractor.UI.BlazorWeb.Services;
var builder = WebAssemblyHostBuilder.CreateDefault(args); // Replace WebApplication.CreateBuilder with WebAssemblyHostBuilder.CreateDefault




builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Base address for the Web API
var apiBaseAddress = builder.Configuration["ApiBaseUrl"] ?? builder.HostEnvironment.BaseAddress; // This line now works because WebAssemblyHostBuilder has HostEnvironment
Console.WriteLine($"Using API base address: {apiBaseAddress}");

// Register HttpClient
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(apiBaseAddress) });

// Register services
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddHotKeys2();
builder.Services.AddScoped<ApiClientService>();
builder.Services.AddScoped<EmrDatabaseService>();
builder.Services.AddScoped<FhirResourceService>();
builder.Services.AddScoped<MappingService>();
builder.Services.AddScoped<LocalStorageService>();

await builder.Build().RunAsync();

//var app = builder.Build();

//// Configure the HTTP request pipeline.
//if (!app.Environment.IsDevelopment())
//{
//    app.UseExceptionHandler("/Error", createScopeForErrors: true);
//    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
//    app.UseHsts();
//}

//app.UseHttpsRedirection();


//app.UseAntiforgery();

//app.MapStaticAssets();
//app.MapRazorComponents<App>()
//    .AddInteractiveServerRenderMode();

//app.Run();
