using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using PosmatraciApp;
using PosmatraciApp.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddMudServices();
builder.Services.AddScoped<StorageService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<LocationService>();
builder.Services.AddScoped<ChecklistService>();
builder.Services.AddScoped<QrEncoderService>();

await builder.Build().RunAsync();
