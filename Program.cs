using Boodschap.Components;
using Boodschap.Data;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var sqliteConnectionString = StoreConfiguration.NormalizeSqliteConnectionString(
    builder.Configuration.GetConnectionString("Boodschap"),
    builder.Environment.ContentRootPath);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddDbContextFactory<BoodschapDbContext>(options =>
    options.UseSqlite(sqliteConnectionString));
builder.Services.AddSingleton<StoreChangeNotifier>();
builder.Services.AddScoped<Store>();

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

await StoreInitializer.InitializeAsync(app.Services);

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
