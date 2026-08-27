using System.Net.Http.Json;
using System.Text.Json;
using Boodschap.Features.Recipes.Application;
using Microsoft.Extensions.Options;

namespace Boodschap.Features.Recipes.Infrastructure.Integration;

public sealed class N8nRecipeSearchService(
	HttpClient httpClient,
	IOptions<N8nRecipeSearchOptions> options) : IRecipeSearchService
{
	private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
	{
		PropertyNameCaseInsensitive = true
	};

	private readonly string webhookUrl = options.Value.WebhookUrl;

	public async Task<IReadOnlyList<RecipeSuggestion>> SearchAsync(
		RecipeSearchRequest request,
		CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrWhiteSpace(webhookUrl))
		{
			throw new InvalidOperationException("Recipes:N8n:WebhookUrl is not configured.");
		}

		var normalizedIngredients = request.Ingredients
			.Select(ingredient => ingredient.Trim())
			.Where(ingredient => !string.IsNullOrWhiteSpace(ingredient))
			.Distinct(StringComparer.CurrentCultureIgnoreCase)
			.ToList();

		using var response = await httpClient.PostAsJsonAsync(
			webhookUrl,
			new N8nRecipeSearchPayload(normalizedIngredients, request.MealType, request.MaxResults),
			SerializerOptions,
			cancellationToken);

		response.EnsureSuccessStatusCode();

		await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
		var payload = await JsonSerializer.DeserializeAsync<N8nRecipeSearchResponse>(
			contentStream,
			SerializerOptions,
			cancellationToken);

		return payload?.Recipes?
			.Where(recipe => !string.IsNullOrWhiteSpace(recipe.Title) && recipe.Ingredients.Count > 0)
			.Select(recipe => new RecipeSuggestion
			{
				Title = recipe.Title.Trim(),
				Why = string.IsNullOrWhiteSpace(recipe.Why) ? null : recipe.Why.Trim(),
				Ingredients = recipe.Ingredients
					.Select(ingredient => ingredient.Trim())
					.Where(ingredient => !string.IsNullOrWhiteSpace(ingredient))
					.ToArray(),
				Steps = recipe.Steps
					.Select(step => step.Trim())
					.Where(step => !string.IsNullOrWhiteSpace(step))
					.ToArray()
			})
			.ToList() ?? [];
	}

	private sealed record N8nRecipeSearchPayload(
		IReadOnlyList<string> Ingredients,
		string? MealType,
		int MaxResults);

	private sealed record N8nRecipeSearchResponse
	{
		public IReadOnlyList<RecipeSuggestion> Recipes { get; init; } = [];
	}
}