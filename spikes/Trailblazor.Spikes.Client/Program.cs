using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Trailblazor.Spikes.Client.Dock;
using Trailblazor.Spikes.Client.Grid;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddTrailblazor();
builder.Services.AddScoped<PanelStats>();
builder.Services.AddScoped<GridStats>();

await builder.Build().RunAsync();
