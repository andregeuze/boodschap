using System.Globalization;
using Boodschap.Features.Nutrition.Application;
using Boodschap.Features.Nutrition.Domain;
using Boodschap.Features.Recipes;
using Boodschap.Features.Recipes.Application;
using Boodschap.Features.Recipes.Presentation.Pages;
using Bunit;
using Microsoft.Extensions.DependencyInjection;

namespace Boodschap.Features.Recipes.Tests;

public sealed class RecipesPageComponentTests
{
	[Fact]
	public void Render_ShowsCompactTabsAndKnownNutritionIngredients()
	{
		using var context = CreateContext();

		var cut = context.Render<RecipesPage>();

		cut.WaitForAssertion(() =>
		{
			Assert.Contains("fav", cut.Markup);
			Assert.Contains("Romige kokoskip", cut.Markup);
			Assert.Contains("Kipfilet (vleeswaar)", cut.Markup);
			Assert.DoesNotContain("Profielvoorkeuren", cut.Markup);
		});
	}

	[Fact]
	public void ClickingLunchFilter_SwitchesVisibleDraft()
	{
		using var context = CreateContext();

		var cut = context.Render<RecipesPage>();
		cut.WaitForAssertion(() => Assert.Contains("lunch", cut.Markup));
		cut.FindAll("button").Single(button => button.TextContent.Trim() == "lunch").Click();

		cut.WaitForAssertion(() =>
		{
			Assert.Contains("Snelle noedelkom", cut.Markup);
			Assert.Contains("Ei gekookt", cut.Markup);
		});
	}

	[Fact]
	public void ClickingPlus_StartsNewRecipeDraft()
	{
		using var context = CreateContext();

		var cut = context.Render<RecipesPage>();
		cut.WaitForAssertion(() => Assert.Contains("Nieuw recept", cut.Markup));
		cut.Find("button[aria-label='Nieuw recept']").Click();

		cut.WaitForAssertion(() =>
		{
			Assert.Contains("Nog geen ingredienten.", cut.Markup);
			Assert.Contains("Nieuw recept", cut.Markup);
		});
	}

	[Fact]
	public void ClickingSearchWithAi_RendersReturnedSuggestion()
	{
		using var context = CreateContext();

		var cut = context.Render<RecipesPage>();
		cut.WaitForAssertion(() => Assert.Contains("Zoek met AI", cut.Markup));
		cut.FindAll("button").Single(button => button.TextContent.Contains("Zoek met AI", StringComparison.Ordinal)).Click();

		cut.WaitForAssertion(() =>
		{
			Assert.Contains("AI-suggestie", cut.Markup);
			Assert.Contains("Romige penne met kip", cut.Markup);
			Assert.Contains("Bak de kipfilet kort goudbruin.", cut.Markup);
		});
	}

	[Fact]
	public void NewDraft_AllowsAddingKnownIngredientFromNutritionCatalog()
	{
		using var context = CreateContext();

		var cut = context.Render<RecipesPage>();
		cut.Find("button[aria-label='Nieuw recept']").Click();
		cut.Find("#recipe-ingredient-input").Input("Ei");
		cut.FindAll("button").Single(button => button.TextContent.Trim() == "Toevoegen").Click();

		cut.WaitForAssertion(() => Assert.Contains("Ei gekookt", cut.Markup));
	}

	private static BunitContext CreateContext()
	{
		var culture = CultureInfo.GetCultureInfo("nl-NL");
		CultureInfo.CurrentCulture = culture;
		CultureInfo.CurrentUICulture = culture;

		var context = new BunitContext();
		context.Services.AddLocalization(options => options.ResourcesPath = "Resources");
		context.Services.Configure<RecipeFeatureOptions>(options => options.Enabled = true);
		context.Services.AddSingleton<IFoodService>(new FakeFoodService());
		context.Services.AddSingleton<IRecipeSearchService>(new FakeRecipeSearchService());
		return context;
	}

	private sealed class FakeFoodService : IFoodService
	{
		private static readonly IReadOnlyList<Food> Foods =
		[
			new() { Id = 1, Name = "Kaas Mozzarella gemaakt v koemelk", FoodGroup = "Zuivel" },
			new() { Id = 2, Name = "Kokosroom", FoodGroup = "Overig" },
			new() { Id = 3, Name = "Penne", FoodGroup = "Graanproducten" },
			new() { Id = 4, Name = "Kipfilet (vleeswaar)", FoodGroup = "Vlees" },
			new() { Id = 5, Name = "Courgette gekookt", FoodGroup = "Groente" },
			new() { Id = 6, Name = "Ei gekookt", FoodGroup = "Eieren" }
		];

		public Task<IReadOnlyList<Food>> GetFoodsAsync(CancellationToken cancellationToken = default)
		{
			return Task.FromResult(Foods);
		}

		public Task<IReadOnlyList<Food>> SearchFoodsAsync(string query, CancellationToken cancellationToken = default)
		{
			return Task.FromResult(Foods);
		}

		public Task<FoodImportResult> ImportNevoDetailsAsync(Stream source, CancellationToken cancellationToken = default)
		{
			return Task.FromResult(new FoodImportResult(0));
		}
	}

	private sealed class FakeRecipeSearchService : IRecipeSearchService
	{
		public Task<IReadOnlyList<RecipeSuggestion>> SearchAsync(RecipeSearchRequest request, CancellationToken cancellationToken = default)
		{
			IReadOnlyList<RecipeSuggestion> suggestions =
			[
				new RecipeSuggestion
				{
					Title = "Romige penne met kip",
					Why = $"Past goed bij {string.Join(", ", request.Ingredients.Take(3))}.",
					Ingredients = ["Penne", "Kipfilet (vleeswaar)", "Kokosroom"],
					Steps = ["Kook de penne beetgaar.", "Bak de kipfilet kort goudbruin.", "Roer de kokosroom erdoor en serveer direct."]
				}
			];

			return Task.FromResult(suggestions);
		}
	}
}