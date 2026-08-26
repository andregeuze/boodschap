using Boodschap.Features.Nutrition.Application;
using Boodschap.Features.Nutrition.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Boodschap.Features.Nutrition.Tests;

public sealed class NutritionModuleTests
{
	[Fact]
	public void AddNutritionFeature_WhenDisabled_DoesNotRegisterNutritionServices()
	{
		var services = new ServiceCollection();

		services.AddNutritionFeature(CreateConfiguration(isNutritionEnabled: false), "Data Source=:memory:");

		Assert.DoesNotContain(services, service => service.ServiceType == typeof(IFoodService));
		Assert.DoesNotContain(services, service => service.ServiceType == typeof(IDbContextFactory<NutritionDbContext>));
	}

	[Fact]
	public void AddNutritionFeature_WhenEnabled_RegistersNutritionServices()
	{
		var services = new ServiceCollection();

		services.AddNutritionFeature(CreateConfiguration(isNutritionEnabled: true), "Data Source=:memory:");

		Assert.Contains(services, service => service.ServiceType == typeof(IFoodService));
		Assert.Contains(services, service => service.ServiceType == typeof(IDbContextFactory<NutritionDbContext>));
	}

	private static IConfiguration CreateConfiguration(bool isNutritionEnabled)
	{
		return new ConfigurationBuilder()
			.AddInMemoryCollection(new Dictionary<string, string?>
			{
				[$"{NutritionFeatureOptions.SectionName}:Enabled"] = isNutritionEnabled.ToString()
			})
			.Build();
	}
}