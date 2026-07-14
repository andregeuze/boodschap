using Boodschap.Features.Nutrition.Domain;

namespace Boodschap.Features.Nutrition.Application;

public sealed class FoodService(IFoodRepository foodRepository) : IFoodService
{
	public Task<IReadOnlyList<Food>> GetFoodsAsync(CancellationToken cancellationToken = default)
	{
		return foodRepository.GetFoodsAsync(cancellationToken);
	}

	public Task<IReadOnlyList<Food>> SearchFoodsAsync(string query, CancellationToken cancellationToken = default)
	{
		return foodRepository.SearchFoodsAsync(query, cancellationToken);
	}
}