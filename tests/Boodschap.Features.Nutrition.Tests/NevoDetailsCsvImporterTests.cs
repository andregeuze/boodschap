using Boodschap.Features.Nutrition.Infrastructure.Import;

namespace Boodschap.Features.Nutrition.Tests;

public sealed class NevoDetailsCsvImporterTests
{
	[Fact]
	public void ReadFoods_ImportsDetailedNevoCsvFromFixture()
	{
		var importer = new NevoDetailsCsvImporter();
		var fixturePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "NEVO2025_v9.0_Details.csv");

		var foods = importer.ReadFoods(fixturePath);

		Assert.True(foods.Count > 2_000);
		var potatoes = Assert.Single(foods, food => food.NevoCode == "1");
		Assert.Equal("NEVO-Online 2025 9.0", potatoes.NevoVersion);
		Assert.Equal("Aardappelen en knolgewassen", potatoes.FoodGroup);
		Assert.Equal("Potatoes and tubers", potatoes.EnglishFoodGroup);
		Assert.Equal("Aardappelen rauw", potatoes.Name);
		Assert.Equal("Potatoes raw", potatoes.EnglishName);
		Assert.Equal("per 100g", potatoes.Quantity);
		Assert.Equal(88m, potatoes.EnergyKcal);
		Assert.Equal(2m, potatoes.Protein);
		Assert.Equal(19m, potatoes.Carbohydrates);
		Assert.Equal(0m, potatoes.Fat);
		Assert.Equal(1.8m, potatoes.Fiber);
		Assert.True(potatoes.NutrientDetails.Count > 100);

		var potatoEnergy = Assert.Single(potatoes.NutrientDetails, detail => detail.NutrientCode == "ENERCC");
		Assert.Equal("Energie en macronutriënten", potatoEnergy.NutrientGroup);
		Assert.Equal("Energy and macronutrients", potatoEnergy.ComponentGroup);
		Assert.Equal("Energie kcal", potatoEnergy.NutrientName.Trim());
		Assert.Equal("Energy kcal", potatoEnergy.Component.Trim());
		Assert.Equal("88", potatoEnergy.RawValue);
		Assert.Equal(88m, potatoEnergy.Value);
		Assert.Equal("kcal", potatoEnergy.Unit);
		Assert.Equal("MI0115", potatoEnergy.SourceCode);
		Assert.StartsWith("Berekend obv andere voedingsstoffen", potatoEnergy.Reference);

		var pasta = Assert.Single(foods, food => food.NevoCode == "4");
		Assert.Equal("Pasta witte rauw", pasta.Name);
		Assert.Equal(356m, pasta.EnergyKcal);
		Assert.Equal(12.3m, pasta.Protein);
		Assert.Equal(72m, pasta.Carbohydrates);
		Assert.Equal(1.5m, pasta.Fat);
		Assert.Equal(3m, pasta.Fiber);
	}
}