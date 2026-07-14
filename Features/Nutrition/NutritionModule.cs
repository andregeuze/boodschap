using Boodschap.Features.Nutrition.Application;
using Boodschap.Features.Nutrition.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Boodschap.Features.Nutrition;

public static class NutritionModule
{
	public static IServiceCollection AddNutritionFeature(this IServiceCollection services, string sqliteConnectionString)
	{
		services.AddDbContextFactory<NutritionDbContext>(options => options.UseSqlite(
			sqliteConnectionString,
			sqlite => sqlite.MigrationsHistoryTable(NutritionDbContext.MigrationsHistoryTableName)));
		services.AddScoped<IFoodRepository, FoodRepository>();
		services.AddScoped<IFoodService, FoodService>();

		return services;
	}
}