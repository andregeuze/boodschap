using System.Globalization;
using Boodschap.Features.Authentication.Application;
using Boodschap.Features.Authentication.Infrastructure.Remote;
using Boodschap.Features.ShoppingLists.Application;
using Boodschap.Features.ShoppingLists.Infrastructure.Remote;
using Boodschap.Shared.Realtime;
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
			[$"{BackendOptions.SectionName}:BaseUrl"] = BackendOptions.DefaultBaseUrl
		});

		var backendOptions = new BackendOptions
		{
			BaseUrl = builder.Configuration[$"{BackendOptions.SectionName}:BaseUrl"] ?? BackendOptions.DefaultBaseUrl
		};
		var backendUri = backendOptions.GetValidatedBaseUri();

		builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
		builder.Services.AddSingleton(backendOptions);
		builder.Services.AddSingleton<IApiTokenStore, SecureStorageApiTokenStore>();
		builder.Services.AddSingleton<MobileSessionState>();
		builder.Services.AddSingleton<ILocalAuthenticationSession>(services => services.GetRequiredService<MobileSessionState>());
		builder.Services.AddHttpClient<IRemoteAuthenticationClient, RemoteAuthenticationClient>(client => client.BaseAddress = backendUri);
		builder.Services.AddScoped<ILocalAuthenticationService>(services => services.GetRequiredService<IRemoteAuthenticationClient>());
		builder.Services.AddTransient<AuthenticatedHttpMessageHandler>();
		builder.Services.AddHttpClient<IShoppingListService, HttpShoppingListService>(client => client.BaseAddress = backendUri)
			.AddHttpMessageHandler<AuthenticatedHttpMessageHandler>();
		builder.Services.AddSingleton<StoreChangeNotifier>();
		builder.Services.AddSingleton<MobileStoreChangeClient>();
		builder.Services.AddSingleton<AppInitializationService>();
		builder.Services.AddSingleton<MainPage>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
