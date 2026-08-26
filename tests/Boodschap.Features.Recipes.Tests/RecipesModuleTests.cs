using Boodschap.Features.Recipes.Application;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Boodschap.Features.Recipes.Tests;

public sealed class RecipesModuleTests
{
	[Fact]
	public void AddRecipesFeature_WhenDisabled_DoesNotRegisterRecipeSearchService()
	{
		var services = new ServiceCollection();

		services.AddRecipesFeature(CreateConfiguration(isRecipesEnabled: false));

		Assert.DoesNotContain(services, service => service.ServiceType == typeof(IRecipeSearchService));
	}

	[Fact]
	public void AddRecipesFeature_WhenEnabled_RegistersRecipeSearchService()
	{
		var services = new ServiceCollection();

		services.AddRecipesFeature(CreateConfiguration(isRecipesEnabled: true));

		Assert.Contains(services, service => service.ServiceType == typeof(IRecipeSearchService));
	}

	private static IConfiguration CreateConfiguration(bool isRecipesEnabled)
	{
		return new ConfigurationBuilder()
			.AddInMemoryCollection(new Dictionary<string, string?>
			{
				[$"{RecipeFeatureOptions.SectionName}:Enabled"] = isRecipesEnabled.ToString()
			})
			.Build();
	}
}