using Boodschap.Components;
using Boodschap.Features.ShoppingLists;
using Boodschap.Features.ShoppingLists.Infrastructure.Persistence;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);
var sqliteConnectionString = SqliteConnectionStringResolver.Normalize(
    builder.Configuration.GetConnectionString("Boodschap"),
    builder.Environment.ContentRootPath);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

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

await ShoppingListsInitializer.InitializeAsync(app.Services);

app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.MapStaticAssets();
app.UseStaticFiles();
app.UseWebSockets();
app.UseAntiforgery();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
