using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Trailblazor.BrowserTests.Host;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.Services.AddTrailblazor(TestAppOptions.Configure);
await builder.Build().RunAsync();
