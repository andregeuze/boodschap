namespace Boodschap.Features.Nutrition.Domain;

public sealed class Food
{
	public int Id { get; set; }
	public string NevoVersion { get; set; } = string.Empty;
	public string FoodGroup { get; set; } = string.Empty;
	public string EnglishFoodGroup { get; set; } = string.Empty;
	public string NevoCode { get; set; } = string.Empty;
	public string Name { get; set; } = string.Empty;
	public string EnglishName { get; set; } = string.Empty;
	public string Quantity { get; set; } = string.Empty;
	public decimal EnergyKcal { get; set; }
	public decimal Protein { get; set; }
	public decimal Carbohydrates { get; set; }
	public decimal Fat { get; set; }
	public decimal Fiber { get; set; }
	public ICollection<FoodNutrientDetail> NutrientDetails { get; set; } = [];

	public static decimal Calculate(decimal per100g, decimal grams)
	{
		return per100g / 100m * grams;
	}

	public FoodPortion CalculatePortion(decimal grams)
	{
		return new FoodPortion(
			FoodId: Id,
			FoodName: Name,
			Grams: grams,
			EnergyKcal: Calculate(EnergyKcal, grams),
			Protein: Calculate(Protein, grams),
			Carbohydrates: Calculate(Carbohydrates, grams),
			Fat: Calculate(Fat, grams),
			Fiber: Calculate(Fiber, grams));
	}
}