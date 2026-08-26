using Boodschap.Features.Nutrition.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Boodschap.Features.Nutrition.Tests;

public sealed class NutritionDevelopmentSeederTests
{
	[Fact]
	public async Task SeedAsync_CreatesFiveBasicFoodsOnce()
	{
		var sqlitePath = Path.Combine(Path.GetTempPath(), $"boodschap-nutrition-dev-{Guid.NewGuid():N}.db");

		try
		{
			using var serviceProvider = CreateServiceProvider(sqlitePath);
			await NutritionInitializer.InitializeAsync(serviceProvider);

			await NutritionDevelopmentSeeder.SeedAsync(serviceProvider);
			await NutritionDevelopmentSeeder.SeedAsync(serviceProvider);

			await using var scope = serviceProvider.CreateAsyncScope();
			var dbContextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<NutritionDbContext>>();
			await using var dbContext = await dbContextFactory.CreateDbContextAsync();
			var foods = await dbContext.Foods.AsNoTracking().OrderBy(food => food.NevoCode).ToListAsync();

			Assert.Collection(
				foods,
				food => AssertSeedFood(food.NevoCode, "DEV-001", food.Name, "Appel"),
				food => AssertSeedFood(food.NevoCode, "DEV-002", food.Name, "Banaan"),
				food => AssertSeedFood(food.NevoCode, "DEV-003", food.Name, "Volkorenbrood"),
				food => AssertSeedFood(food.NevoCode, "DEV-004", food.Name, "Halfvolle melk"),
				food => AssertSeedFood(food.NevoCode, "DEV-005", food.Name, "Ei gekookt"));
		}
		finally
		{
			DeleteSqliteArtifacts(sqlitePath);
		}
	}

	private static ServiceProvider CreateServiceProvider(string sqlitePath)
	{
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddNutritionFeature(new ConfigurationBuilder().Build(), $"Data Source={sqlitePath}");

		return services.BuildServiceProvider();
	}

	private static void AssertSeedFood(string actualCode, string expectedCode, string actualName, string expectedName)
	{
		Assert.Equal(expectedCode, actualCode);
		Assert.Equal(expectedName, actualName);
	}

	private static void DeleteSqliteArtifacts(string sqlitePath)
	{
		DeleteIfExists(sqlitePath);
		DeleteIfExists($"{sqlitePath}-shm");
		DeleteIfExists($"{sqlitePath}-wal");
	}

	private static void DeleteIfExists(string path)
	{
		if (!File.Exists(path))
		{
			return;
		}

		try
		{
			File.Delete(path);
		}
		catch (IOException)
		{
		}
		catch (UnauthorizedAccessException)
		{
		}
	}
}