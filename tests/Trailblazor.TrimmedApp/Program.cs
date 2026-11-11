using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Trailblazor;
using Trailblazor.TrimmedApp;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.Services.AddTrailblazor();

await builder.Build().RunAsync();
