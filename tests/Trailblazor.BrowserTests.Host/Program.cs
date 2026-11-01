using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Trailblazor.BrowserTests.Host;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddTrailblazor(options => options.Dialogs.OutsidePopups.Add(".reused-popup"));

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
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.Run();
