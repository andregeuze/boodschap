using Boodschap.Features.Nutrition.Domain;
using Boodschap.Features.Nutrition.Application;
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using System.Text;

namespace Boodschap.Features.Nutrition.Infrastructure.Import;

public sealed class NevoDetailsCsvImporter : INevoFoodImporter
{
	private static readonly CultureInfo DutchCulture = CultureInfo.GetCultureInfo("nl-NL");

	public IReadOnlyList<Food> ReadFoods(string path)
	{
		using var stream = File.OpenRead(path);
		return ReadFoods(stream);
	}

	public IReadOnlyList<Food> ReadFoods(Stream source)
	{
		using var reader = new StreamReader(source, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 1024, leaveOpen: true);
		using var csv = new CsvReader(reader, new CsvConfiguration(DutchCulture)
		{
			Delimiter = "|",
			HasHeaderRecord = true,
			TrimOptions = TrimOptions.Trim
		});
		var foods = new Dictionary<string, FoodImportRow>(StringComparer.Ordinal);

		csv.Read();
		csv.ReadHeader();

		while (csv.Read())
		{
			var nevoCode = GetValue(csv, "NEVO-code");
			if (string.IsNullOrWhiteSpace(nevoCode))
			{
				continue;
			}

			var food = GetOrCreateFood(csv, foods, nevoCode);
			var nutrientCode = GetValue(csv, "Nutrient-code");
			var rawValue = GetValue(csv, "Gehalte/Value");
			var value = ParseDecimalOrZero(rawValue);

			food.NutrientDetails.Add(new FoodNutrientDetail
			{
				NutrientGroup = GetValue(csv, "Voedingsstofgroep"),
				ComponentGroup = GetValue(csv, "Component group"),
				NutrientCode = nutrientCode,
				NutrientName = GetValue(csv, "Voedingsstof"),
				Component = GetValue(csv, "Component"),
				RawValue = rawValue,
				Value = value,
				Unit = GetValue(csv, "Eenheid/Unit"),
				TraceFortified = GetValue(csv, "Spoor / Verrijkt/Trace / Fortified"),
				SourceCode = GetValue(csv, "Broncode/Source code"),
				Reference = GetValue(csv, "Referentie/Reference")
			});

			ApplyNutrientValue(food, nutrientCode, value);
		}

		return [.. foods.Values
			.Select(food => food.ToFood())
			.OrderBy(food => food.Name)];
	}

	private static string GetValue(CsvReader csv, string columnName)
	{
		return csv.GetField(columnName)?.Trim() ?? string.Empty;
	}

	private static FoodImportRow GetOrCreateFood(
		CsvReader csv,
		IDictionary<string, FoodImportRow> foods,
		string nevoCode)
	{
		if (foods.TryGetValue(nevoCode, out var food))
		{
			return food;
		}

		food = new FoodImportRow(
			NevoVersion: GetValue(csv, "NEVO-versie/NEVO-version"),
			FoodGroup: GetValue(csv, "Voedingsmiddelgroep"),
			EnglishFoodGroup: GetValue(csv, "Food group"),
			NevoCode: nevoCode,
			Name: GetValue(csv, "Voedingsmiddelnaam/Dutch food name"),
			EnglishName: GetValue(csv, "Engelse naam/Food name"),
			Quantity: GetValue(csv, "Hoeveelheid/Quantity"));
		foods.Add(nevoCode, food);
		return food;
	}

	private static decimal ParseDecimalOrZero(string value)
	{
		return decimal.TryParse(value, NumberStyles.Number, DutchCulture, out var result)
			? result
			: 0m;
	}

	private static void ApplyNutrientValue(FoodImportRow food, string nutrientCode, decimal value)
	{
		switch (nutrientCode.Trim())
		{
			case "ENERCC":
				food.EnergyKcal = value;
				break;
			case "PROT":
				food.Protein = value;
				break;
			case "CHO":
				food.Carbohydrates = value;
				break;
			case "FAT":
				food.Fat = value;
				break;
			case "FIBT":
				food.Fiber = value;
				break;
		}
	}

	private sealed class FoodImportRow(
		string NevoVersion,
		string FoodGroup,
		string EnglishFoodGroup,
		string NevoCode,
		string Name,
		string EnglishName,
		string Quantity)
	{
		public decimal EnergyKcal { get; set; }
		public decimal Protein { get; set; }
		public decimal Carbohydrates { get; set; }
		public decimal Fat { get; set; }
		public decimal Fiber { get; set; }
		public ICollection<FoodNutrientDetail> NutrientDetails { get; } = [];

		public Food ToFood()
		{
			return new Food
			{
				NevoVersion = NevoVersion,
				FoodGroup = FoodGroup,
				EnglishFoodGroup = EnglishFoodGroup,
				NevoCode = NevoCode,
				Name = Name,
				EnglishName = EnglishName,
				Quantity = Quantity,
				EnergyKcal = EnergyKcal,
				Protein = Protein,
				Carbohydrates = Carbohydrates,
				Fat = Fat,
				Fiber = Fiber,
				NutrientDetails = NutrientDetails
			};
		}
	}
}