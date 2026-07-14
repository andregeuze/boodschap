using Boodschap.Features.Nutrition.Domain;
using Microsoft.EntityFrameworkCore;

namespace Boodschap.Features.Nutrition.Infrastructure.Persistence;

public static class NutritionDevelopmentSeeder
{
	public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
	{
		await using var scope = services.CreateAsyncScope();
		var dbContextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<NutritionDbContext>>();
		await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

		if (await dbContext.Foods.AnyAsync(cancellationToken))
		{
			return;
		}

		dbContext.Foods.AddRange(CreateFoods());
		await dbContext.SaveChangesAsync(cancellationToken);
	}

	private static Food[] CreateFoods()
	{
		return
		[
			new Food
			{
				NevoVersion = "Development seed",
				FoodGroup = "Fruit",
				EnglishFoodGroup = "Fruit",
				NevoCode = "DEV-001",
				Name = "Appel",
				EnglishName = "Apple",
				Quantity = "per 100g",
				EnergyKcal = 52m,
				Protein = 0.3m,
				Carbohydrates = 14m,
				Fat = 0.2m,
				Fiber = 2.4m
			},
			new Food
			{
				NevoVersion = "Development seed",
				FoodGroup = "Fruit",
				EnglishFoodGroup = "Fruit",
				NevoCode = "DEV-002",
				Name = "Banaan",
				EnglishName = "Banana",
				Quantity = "per 100g",
				EnergyKcal = 89m,
				Protein = 1.1m,
				Carbohydrates = 23m,
				Fat = 0.3m,
				Fiber = 2.6m
			},
			new Food
			{
				NevoVersion = "Development seed",
				FoodGroup = "Graanproducten",
				EnglishFoodGroup = "Grain products",
				NevoCode = "DEV-003",
				Name = "Volkorenbrood",
				EnglishName = "Whole wheat bread",
				Quantity = "per 100g",
				EnergyKcal = 247m,
				Protein = 13m,
				Carbohydrates = 41m,
				Fat = 4.2m,
				Fiber = 7m
			},
			new Food
			{
				NevoVersion = "Development seed",
				FoodGroup = "Zuivel",
				EnglishFoodGroup = "Dairy",
				NevoCode = "DEV-004",
				Name = "Halfvolle melk",
				EnglishName = "Semi-skimmed milk",
				Quantity = "per 100g",
				EnergyKcal = 47m,
				Protein = 3.4m,
				Carbohydrates = 4.8m,
				Fat = 1.5m,
				Fiber = 0m
			},
			new Food
			{
				NevoVersion = "Development seed",
				FoodGroup = "Eieren",
				EnglishFoodGroup = "Eggs",
				NevoCode = "DEV-005",
				Name = "Ei gekookt",
				EnglishName = "Boiled egg",
				Quantity = "per 100g",
				EnergyKcal = 155m,
				Protein = 13m,
				Carbohydrates = 1.1m,
				Fat = 11m,
				Fiber = 0m
			}
		];
	}
}