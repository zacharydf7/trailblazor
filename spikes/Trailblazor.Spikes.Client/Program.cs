using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Trailblazor.Spikes.Client.Dock;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddTrailblazor();
builder.Services.AddScoped<PanelStats>();

await builder.Build().RunAsync();
