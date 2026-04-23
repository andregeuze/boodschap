using Boodschap.Features.ShoppingLists.Application;
using Boodschap.Features.ShoppingLists.Infrastructure.Persistence;
using Boodschap.Shared.Realtime;
using Microsoft.EntityFrameworkCore;

namespace Boodschap.Features.ShoppingLists;

public static class ShoppingListsModule
{
	public static IServiceCollection AddShoppingListsFeature(this IServiceCollection services, string sqliteConnectionString)
	{
		services.AddDbContextFactory<BoodschapDbContext>(options => options.UseSqlite(sqliteConnectionString));
		services.AddSingleton<StoreChangeNotifier>();
		services.AddScoped<IShoppingListRepository, ShoppingListRepository>();
		services.AddScoped<IShoppingListService, ShoppingListService>();

		return services;
	}
}