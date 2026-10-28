using Trailblazor.Spikes;
using Trailblazor.Spikes.Client.Dock;
using Trailblazor.Spikes.Client.Grid;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents().AddInteractiveServerComponents().AddInteractiveWebAssemblyComponents();
builder.Services.AddTrailblazor();
builder.Services.AddScoped<PanelStats>();
builder.Services.AddScoped<GridStats>();

var app = builder.Build();

app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Trailblazor.Spikes.Client.Routes).Assembly);

app.Run();
