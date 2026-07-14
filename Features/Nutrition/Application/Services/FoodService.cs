using Boodschap.Features.Nutrition.Domain;

namespace Boodschap.Features.Nutrition.Application;

public sealed class FoodService(IFoodRepository foodRepository, INevoFoodImporter nevoFoodImporter) : IFoodService
{
	public Task<IReadOnlyList<Food>> GetFoodsAsync(CancellationToken cancellationToken = default)
	{
		return foodRepository.GetFoodsAsync(cancellationToken);
	}

	public Task<IReadOnlyList<Food>> SearchFoodsAsync(string query, CancellationToken cancellationToken = default)
	{
		return foodRepository.SearchFoodsAsync(query, cancellationToken);
	}

	public async Task<FoodImportResult> ImportNevoDetailsAsync(Stream source, CancellationToken cancellationToken = default)
	{
		await using var bufferedSource = new MemoryStream();
		await source.CopyToAsync(bufferedSource, cancellationToken);
		bufferedSource.Position = 0;

		var foods = nevoFoodImporter.ReadFoods(bufferedSource);
		await foodRepository.UpsertFoodsAsync(foods, cancellationToken);

		return new FoodImportResult(foods.Count);
	}
}