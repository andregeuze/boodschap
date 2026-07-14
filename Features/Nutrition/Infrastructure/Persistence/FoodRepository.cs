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
}