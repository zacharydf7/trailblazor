using System.Collections.Concurrent;
using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Trailblazor.BrowserTests.Host;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents().AddInteractiveServerComponents().AddInteractiveWebAssemblyComponents();
builder.Services.AddTrailblazor(TestAppOptions.Configure);

var app = builder.Build();

// A test opens a page in another culture with ?culture=de-DE; every other page keeps the machine's.
var culture = CultureInfo.CurrentCulture;
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(culture),
    SupportedCultures = [culture, new CultureInfo("de-DE")],
    SupportedUICultures = [culture, new CultureInfo("de-DE")],
    RequestCultureProviders = [new QueryStringRequestCultureProvider()],
});
app.UseAntiforgery();
app.MapStaticAssets();
// A style sheet that takes `wait` milliseconds to arrive and holds the rules in `css`, if any. With `gate`, it arrives
// once /open-gate has been asked for that gate, however long that takes.
var gates = new ConcurrentDictionary<string, TaskCompletionSource>();
app.MapGet("/slow.css", async (int wait, string? css, string? gate, CancellationToken gone) =>
{
    await (gate is null ? Task.Delay(wait, gone) : gates.GetOrAdd(gate, _ => new()).Task.WaitAsync(gone));
    return Results.Text(css ?? "", "text/css");
});
app.MapGet("/open-gate", (string gate) => gates.GetOrAdd(gate, _ => new()).TrySetResult());
app.MapRazorComponents<App>().AddInteractiveServerRenderMode().AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Routes).Assembly);

app.Run();
