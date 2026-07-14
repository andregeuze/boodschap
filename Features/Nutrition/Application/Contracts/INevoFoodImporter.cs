using Boodschap.Features.Nutrition.Domain;

namespace Boodschap.Features.Nutrition.Application;

public interface INevoFoodImporter
{
	IReadOnlyList<Food> ReadFoods(Stream source);
}