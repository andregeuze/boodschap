namespace Boodschap.Features.Nutrition.Domain;

public sealed record FoodPortion(
	int FoodId,
	string FoodName,
	decimal Grams,
	decimal EnergyKcal,
	decimal Protein,
	decimal Carbohydrates,
	decimal Fat,
	decimal Fiber);