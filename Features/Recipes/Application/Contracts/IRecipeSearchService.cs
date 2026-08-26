namespace Boodschap.Features.Recipes.Application;

public interface IRecipeSearchService
{
	Task<IReadOnlyList<RecipeSuggestion>> SearchAsync(
		RecipeSearchRequest request,
		CancellationToken cancellationToken = default);
}