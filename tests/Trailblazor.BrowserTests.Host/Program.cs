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
// A style sheet that takes `wait` milliseconds to arrive and holds the rules in `css`, if any.
app.MapGet("/slow.css", async (int wait, string? css, CancellationToken gone) =>
{
    await Task.Delay(wait, gone);
    return Results.Text(css ?? "", "text/css");
});
app.MapRazorComponents<App>().AddInteractiveServerRenderMode().AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Routes).Assembly);

app.Run();
