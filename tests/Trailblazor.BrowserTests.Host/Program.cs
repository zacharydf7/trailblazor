using System.Collections.Concurrent;
using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Trailblazor.BrowserTests.Host;

var builder = WebApplication.CreateBuilder(args);

// A test that cuts a page's connection counts on finding its circuit again a few seconds later. A full run ends
// thousands of circuits a minute, so the hundred disconnected ones a server keeps by default go at once; they are
// kept for less time instead, so no more of them pile up.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents(options =>
    {
        options.DisconnectedCircuitMaxRetained = 100_000;
        options.DisconnectedCircuitRetentionPeriod = TimeSpan.FromSeconds(30);
    })
    .AddInteractiveWebAssemblyComponents();
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
// A style sheet that takes `wait` milliseconds to arrive and holds the rules in `css`, if any. With `gate`, it ignores
// `wait` and arrives once /open-gate is requested with the same gate, however long that takes.
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
