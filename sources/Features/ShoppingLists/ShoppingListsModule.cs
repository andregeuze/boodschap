using Boodschap.Features.ShoppingLists.Application;
using Boodschap.Features.ShoppingLists.Infrastructure.Mcp;
using Boodschap.Features.ShoppingLists.Infrastructure.Persistence;
using Boodschap.Features.ShoppingLists.Presentation;
using Boodschap.Shared.Realtime;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;

namespace Boodschap.Features.ShoppingLists;

public static class ShoppingListsModule
{
	public static IServiceCollection AddShoppingListsFeature(this IServiceCollection services, string sqliteConnectionString)
	{
		services.AddDbContextFactory<BoodschapDbContext>(options => options.UseSqlite(
			sqliteConnectionString,
			sqlite => sqlite.MigrationsHistoryTable(BoodschapDbContext.MigrationsHistoryTableName)));
		services.AddSingleton<StoreChangeNotifier>();
		services.AddScoped<IShoppingListRepository, ShoppingListRepository>();
		services.AddScoped<IShoppingListService, ShoppingListService>();
		services.AddSignalR();
		services.AddHostedService<StoreChangeBroadcastService>();
		services.AddAuthentication()
			.AddScheme<AuthenticationSchemeOptions, McpAccessKeyAuthenticationHandler>(
				ShoppingListsMcpDefaults.AuthenticationScheme,
				_ => { });

		return services;
	}

	public static IEndpointRouteBuilder MapShoppingListsFeature(this IEndpointRouteBuilder endpoints)
	{
		endpoints.MapShoppingListApiEndpoints();
		endpoints.MapShoppingListUpdatesHub();
		return endpoints;
	}
}