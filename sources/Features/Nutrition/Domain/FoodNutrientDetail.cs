namespace Boodschap.Features.Nutrition.Domain;

public sealed class FoodNutrientDetail
{
	public int Id { get; set; }
	public int FoodId { get; set; }
	public Food Food { get; set; } = null!;
	public string NutrientGroup { get; set; } = string.Empty;
	public string ComponentGroup { get; set; } = string.Empty;
	public string NutrientCode { get; set; } = string.Empty;
	public string NutrientName { get; set; } = string.Empty;
	public string Component { get; set; } = string.Empty;
	public string RawValue { get; set; } = string.Empty;
	public decimal Value { get; set; }
	public string Unit { get; set; } = string.Empty;
	public string TraceFortified { get; set; } = string.Empty;
	public string SourceCode { get; set; } = string.Empty;
	public string Reference { get; set; } = string.Empty;
}