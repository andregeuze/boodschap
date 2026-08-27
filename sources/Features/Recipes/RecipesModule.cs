using Boodschap.Features.Recipes.Application;
using Boodschap.Features.Recipes.Infrastructure.Integration;
using Microsoft.Extensions.Options;

namespace Boodschap.Features.Recipes;

public static class RecipesModule
{
	public static IServiceCollection AddRecipesFeature(this IServiceCollection services, IConfiguration configuration)
	{
		services.Configure<RecipeFeatureOptions>(configuration.GetSection(RecipeFeatureOptions.SectionName));

		if (!configuration.IsRecipesFeatureEnabled())
		{
			return services;
		}

		services.Configure<N8nRecipeSearchOptions>(configuration.GetSection(N8nRecipeSearchOptions.SectionName));
		services.AddHttpClient<IRecipeSearchService, N8nRecipeSearchService>((serviceProvider, client) =>
		{
			var options = serviceProvider.GetRequiredService<IOptions<N8nRecipeSearchOptions>>().Value;
			client.Timeout = options.GetTimeout();
		});

		return services;
	}

	public static bool IsRecipesFeatureEnabled(this IConfiguration configuration)
	{
		return configuration.GetValue($"{RecipeFeatureOptions.SectionName}:Enabled", false);
	}
}