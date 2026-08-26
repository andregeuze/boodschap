namespace Boodschap.Features.Recipes.Application;

public sealed record RecipeSuggestion
{
	public string Title { get; init; } = string.Empty;
	public string? Why { get; init; }
	public IReadOnlyList<string> Ingredients { get; init; } = [];
	public IReadOnlyList<string> Steps { get; init; } = [];
}