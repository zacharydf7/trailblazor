using System.Globalization;
using System.Runtime.InteropServices.JavaScript;
using System.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Trailblazor.BrowserTests.Host;

// The host's culture comes in ?culture=, which BrowserTest adds as it asks for WebAssembly, and which is empty for the
// invariant culture; without it, WebAssembly would take the browser's.
var search = OperatingSystem.IsBrowser()
    ? JSHost.GlobalThis.GetPropertyAsJSObject("location")?.GetPropertyAsString("search") : null;
var name = HttpUtility.ParseQueryString(search ?? "")["culture"];
var culture = string.IsNullOrEmpty(name) ? CultureInfo.InvariantCulture : new CultureInfo(name);
CultureInfo.DefaultThreadCurrentCulture = culture;
CultureInfo.DefaultThreadCurrentUICulture = culture;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.Services.AddTrailblazor(TestAppOptions.Configure);
await builder.Build().RunAsync();
