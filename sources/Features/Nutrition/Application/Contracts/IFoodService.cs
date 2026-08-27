using Boodschap.Features.Nutrition.Domain;

namespace Boodschap.Features.Nutrition.Application;

public interface IFoodService
{
	Task<IReadOnlyList<Food>> GetFoodsAsync(CancellationToken cancellationToken = default);
	Task<IReadOnlyList<Food>> SearchFoodsAsync(string query, CancellationToken cancellationToken = default);
	Task<FoodImportResult> ImportNevoDetailsAsync(Stream source, CancellationToken cancellationToken = default);
}