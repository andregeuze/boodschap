using Boodschap.Features.Nutrition.Application;
using Boodschap.Features.Nutrition.Infrastructure.Import;
using Boodschap.Features.Nutrition.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Boodschap.Features.Nutrition;

public static class NutritionModule
{
	public static IServiceCollection AddNutritionFeature(this IServiceCollection services, IConfiguration configuration, string sqliteConnectionString)
	{
		services.Configure<NutritionFeatureOptions>(configuration.GetSection(NutritionFeatureOptions.SectionName));

		if (!configuration.IsNutritionFeatureEnabled())
		{
			return services;
		}

		services.AddDbContextFactory<NutritionDbContext>(options => options.UseSqlite(
			sqliteConnectionString,
			sqlite => sqlite.MigrationsHistoryTable(NutritionDbContext.MigrationsHistoryTableName)));
		services.AddSingleton<INevoFoodImporter, NevoDetailsCsvImporter>();
		services.AddScoped<IFoodRepository, FoodRepository>();
		services.AddScoped<IFoodService, FoodService>();

		return services;
	}

	public static bool IsNutritionFeatureEnabled(this IConfiguration configuration)
	{
		return configuration.GetValue($"{NutritionFeatureOptions.SectionName}:Enabled", true);
	}
}