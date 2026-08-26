namespace Boodschap.Features.Recipes.Application;

public sealed record RecipeSearchRequest(
	IReadOnlyList<string> Ingredients,
	string? MealType,
	int MaxResults = 1);