using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Boodschap.Features.Recipes.Application;
using Boodschap.Features.Recipes.Infrastructure.Integration;
using Microsoft.Extensions.Options;

namespace Boodschap.Features.Recipes.Tests;

public sealed class N8nRecipeSearchServiceTests
{
	[Fact]
	public async Task SearchAsync_PostsNormalizedPayloadAndParsesRecipes()
	{
		JsonElement? sentPayload = null;
		var handler = new StubHttpMessageHandler(async request =>
		{
			Assert.Equal(HttpMethod.Post, request.Method);
			Assert.Equal("http://localhost:5678/webhook-test/recepten", request.RequestUri?.ToString());

			sentPayload = await request.Content!.ReadFromJsonAsync<JsonElement>();

			return new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = JsonContent.Create(new
				{
					recipes = new[]
					{
						new
						{
							title = "Penne met kip",
							why = "Gebruikt je basisingredienten slim.",
							ingredients = new[] { "Penne", "Kipfilet" },
							steps = new[] { "Kook de penne.", "Bak de kipfilet." }
						}
					}
				})
			};
		});

		var service = new N8nRecipeSearchService(
			new HttpClient(handler),
			Options.Create(new N8nRecipeSearchOptions
			{
				WebhookUrl = "http://localhost:5678/webhook-test/recepten",
				TimeoutSeconds = 180
			}));

		var results = await service.SearchAsync(new RecipeSearchRequest([" Penne ", "Kipfilet", "penne"], "dinner", 1));

		Assert.NotNull(sentPayload);
		Assert.Equal(["Penne", "Kipfilet"], sentPayload.Value.GetProperty("ingredients").EnumerateArray().Select(item => item.GetString() ?? string.Empty).ToArray());
		Assert.Equal("dinner", sentPayload.Value.GetProperty("mealType").GetString());
		Assert.Equal(1, sentPayload.Value.GetProperty("maxResults").GetInt32());

		var suggestion = Assert.Single(results);
		Assert.Equal("Penne met kip", suggestion.Title);
		Assert.Equal("Gebruikt je basisingredienten slim.", suggestion.Why);
		Assert.Equal(["Penne", "Kipfilet"], suggestion.Ingredients);
		Assert.Equal(["Kook de penne.", "Bak de kipfilet."], suggestion.Steps);
	}

	private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler) : HttpMessageHandler
	{
		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			return handler(request);
		}
	}
}