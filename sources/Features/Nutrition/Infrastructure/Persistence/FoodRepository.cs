using Boodschap.Features.Nutrition.Application;
using Boodschap.Features.Nutrition.Domain;
using Microsoft.EntityFrameworkCore;

namespace Boodschap.Features.Nutrition.Infrastructure.Persistence;

public sealed class FoodRepository(IDbContextFactory<NutritionDbContext> dbContextFactory) : IFoodRepository
{
	public async Task<IReadOnlyList<Food>> GetFoodsAsync(CancellationToken cancellationToken = default)
	{
		await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
		return await dbContext.Foods
			.AsNoTracking()
			.OrderBy(food => food.Name)
			.ToListAsync(cancellationToken);
	}

	public async Task<IReadOnlyList<Food>> SearchFoodsAsync(string query, CancellationToken cancellationToken = default)
	{
		var normalizedQuery = query.Trim();
		if (string.IsNullOrWhiteSpace(normalizedQuery))
		{
			return await GetFoodsAsync(cancellationToken);
		}

		await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
		return await dbContext.Foods
			.AsNoTracking()
			.Where(food => EF.Functions.Like(food.Name, $"%{normalizedQuery}%"))
			.OrderBy(food => food.Name)
			.ToListAsync(cancellationToken);
	}

	public async Task UpsertFoodsAsync(IReadOnlyCollection<Food> foods, CancellationToken cancellationToken = default)
	{
		if (foods.Count == 0)
		{
			return;
		}

		await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
		var existingFoods = await dbContext.Foods
			.Include(food => food.NutrientDetails)
			.ToDictionaryAsync(food => food.NevoCode, StringComparer.Ordinal, cancellationToken);

		foreach (var food in foods)
		{
			if (existingFoods.TryGetValue(food.NevoCode, out var existingFood))
			{
				UpdateFood(existingFood, food);
				dbContext.FoodNutrientDetails.RemoveRange(existingFood.NutrientDetails);
				existingFood.NutrientDetails.Clear();

				foreach (var detail in food.NutrientDetails)
				{
					existingFood.NutrientDetails.Add(CloneDetail(detail));
				}

				continue;
			}

			dbContext.Foods.Add(CloneFood(food));
		}

		await dbContext.SaveChangesAsync(cancellationToken);
	}

	private static Food CloneFood(Food food)
	{
		var clone = new Food();
		UpdateFood(clone, food);

		foreach (var detail in food.NutrientDetails)
		{
			clone.NutrientDetails.Add(CloneDetail(detail));
		}

		return clone;
	}

	private static void UpdateFood(Food target, Food source)
	{
		target.NevoVersion = source.NevoVersion;
		target.FoodGroup = source.FoodGroup;
		target.EnglishFoodGroup = source.EnglishFoodGroup;
		target.NevoCode = source.NevoCode;
		target.Name = source.Name;
		target.EnglishName = source.EnglishName;
		target.Quantity = source.Quantity;
		target.EnergyKcal = source.EnergyKcal;
		target.Protein = source.Protein;
		target.Carbohydrates = source.Carbohydrates;
		target.Fat = source.Fat;
		target.Fiber = source.Fiber;
	}

	private static FoodNutrientDetail CloneDetail(FoodNutrientDetail detail)
	{
		return new FoodNutrientDetail
		{
			NutrientGroup = detail.NutrientGroup,
			ComponentGroup = detail.ComponentGroup,
			NutrientCode = detail.NutrientCode,
			NutrientName = detail.NutrientName,
			Component = detail.Component,
			RawValue = detail.RawValue,
			Value = detail.Value,
			Unit = detail.Unit,
			TraceFortified = detail.TraceFortified,
			SourceCode = detail.SourceCode,
			Reference = detail.Reference
		};
	}
}