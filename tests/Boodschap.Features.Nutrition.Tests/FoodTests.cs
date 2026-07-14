using Boodschap.Features.Nutrition.Domain;

namespace Boodschap.Features.Nutrition.Tests;

public sealed class FoodTests
{
	[Theory]
	[InlineData("250", "40", "100")]
	[InlineData("12.5", "80", "10.0")]
	[InlineData("3.2", "0", "0")]
	public void Calculate_ReturnsValueForGrams(string per100gValue, string gramsValue, string expectedValue)
	{
		var per100g = decimal.Parse(per100gValue);
		var grams = decimal.Parse(gramsValue);
		var expected = decimal.Parse(expectedValue);

		var result = Food.Calculate(per100g, grams);

		Assert.Equal(expected, result);
	}

	[Fact]
	public void CalculatePortion_UsesFoodValuesPer100Grams()
	{
		var food = new Food
		{
			Id = 7,
			NevoCode = "1234",
			Name = "Havermout",
			EnergyKcal = 372m,
			Protein = 13.1m,
			Carbohydrates = 60m,
			Fat = 7m,
			Fiber = 9.7m
		};

		var portion = food.CalculatePortion(50m);

		Assert.Equal(7, portion.FoodId);
		Assert.Equal("Havermout", portion.FoodName);
		Assert.Equal(50m, portion.Grams);
		Assert.Equal(186m, portion.EnergyKcal);
		Assert.Equal(6.55m, portion.Protein);
		Assert.Equal(30m, portion.Carbohydrates);
		Assert.Equal(3.5m, portion.Fat);
		Assert.Equal(4.85m, portion.Fiber);
	}
}