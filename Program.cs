using Boodschap.Components;
using Boodschap.Features.Authentication;
using Boodschap.Features.Authentication.Infrastructure.Persistence;
using Boodschap.Features.ShoppingLists;
using Boodschap.Features.ShoppingLists.Infrastructure.Persistence;
using Boodschap.Shared.Infrastructure.Persistence;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Localization;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);
var supportedCultures = new[] { new CultureInfo("nl-NL") };

CultureInfo.DefaultThreadCurrentCulture = supportedCultures[0];
CultureInfo.DefaultThreadCurrentUICulture = supportedCultures[0];

var sqliteConnectionString = SqliteConnectionStringResolver.Normalize(
    builder.Configuration.GetConnectionString("Boodschap"),
    builder.Environment.ContentRootPath);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.DefaultRequestCulture = new RequestCulture(supportedCultures[0]);
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
});

builder.Services.AddAuthenticationFeature(sqliteConnectionString);
builder.Services.AddShoppingListsFeature(sqliteConnectionString);

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor
                             | ForwardedHeaders.XForwardedProto
                             | ForwardedHeaders.XForwardedHost;
    // Accept forwarded headers from any proxy (Docker network, load balancer, etc.)
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

await AuthenticationStoreInitializer.InitializeAsync(app.Services);
if (app.Environment.IsDevelopment())
{
    await AuthenticationDevelopmentSeeder.SeedAsync(app.Services);
}
await ShoppingListsInitializer.InitializeAsync(app.Services);

app.UseForwardedHeaders();
app.UseRequestLocalization();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.MapStaticAssets();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.UseWebSockets();
app.UseAntiforgery();
app.MapAuthenticationFeature();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
