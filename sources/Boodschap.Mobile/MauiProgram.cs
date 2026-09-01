using System.Globalization;
using Boodschap.Features.Authentication.Application;
using Boodschap.Features.Authentication.Infrastructure.Remote;
using Boodschap.Features.ShoppingLists.Application;
using Boodschap.Features.ShoppingLists.Infrastructure.Remote;
using Boodschap.Features.Updates;
using Boodschap.Shared.Realtime;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Boodschap.Mobile;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var culture = new CultureInfo("nl-NL");
		CultureInfo.DefaultThreadCurrentCulture = culture;
		CultureInfo.DefaultThreadCurrentUICulture = culture;

		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
		{
			[$"{BackendOptions.SectionName}:BaseUrl"] = BackendOptions.DefaultBaseUrl,
			[$"{UpdateFeatureOptions.SectionName}:Enabled"] = "true",
			[$"{UpdateFeatureOptions.SectionName}:Owner"] = "andregeuze",
			[$"{UpdateFeatureOptions.SectionName}:Repository"] = "boodschap",
			[$"{UpdateFeatureOptions.SectionName}:Branch"] = "main"
		});

		var backendOptions = new BackendOptions
		{
			BaseUrl = builder.Configuration[$"{BackendOptions.SectionName}:BaseUrl"] ?? BackendOptions.DefaultBaseUrl
		};
		var backendUri = backendOptions.GetValidatedBaseUri();

		builder.Services.AddMauiBlazorWebView();
		builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
		builder.Services.AddAuthorizationCore();
		builder.Services.AddCascadingAuthenticationState();
		builder.Services.AddSingleton(backendOptions);
		builder.Services.AddSingleton<IApiTokenStore, SecureStorageApiTokenStore>();
		builder.Services.AddHttpClient<IRemoteAuthenticationClient, RemoteAuthenticationClient>(client => client.BaseAddress = backendUri);
		builder.Services.AddScoped<ILocalAuthenticationService>(services => services.GetRequiredService<IRemoteAuthenticationClient>());
		builder.Services.AddScoped<MobileAuthenticationStateProvider>();
		builder.Services.AddScoped<AuthenticationStateProvider>(services => services.GetRequiredService<MobileAuthenticationStateProvider>());
		builder.Services.AddScoped<ILocalAuthenticationSession>(services => services.GetRequiredService<MobileAuthenticationStateProvider>());
		builder.Services.AddTransient<AuthenticatedHttpMessageHandler>();
		builder.Services.AddHttpClient<IShoppingListService, HttpShoppingListService>(client => client.BaseAddress = backendUri)
			.AddHttpMessageHandler<AuthenticatedHttpMessageHandler>();
		builder.Services.AddSingleton<StoreChangeNotifier>();
		builder.Services.AddScoped<MobileStoreChangeClient>();
		builder.Services.AddUpdatesFeature(builder.Configuration);
		builder.Services.AddScoped<AppInitializationService>();

#if DEBUG
		builder.Logging.AddDebug();
		builder.Services.AddBlazorWebViewDeveloperTools();
#endif

		return builder.Build();
	}
}
