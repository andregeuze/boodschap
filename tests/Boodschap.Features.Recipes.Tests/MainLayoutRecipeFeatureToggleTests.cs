using System.Globalization;
using Boodschap.Components.Layout;
using Boodschap.Features.Authentication.Presentation.Components;
using Boodschap.Features.Nutrition;
using Boodschap.Features.Recipes;
using Bunit;
using Microsoft.Extensions.DependencyInjection;

namespace Boodschap.Features.Recipes.Tests;

public sealed class MainLayoutRecipeFeatureToggleTests
{
	[Theory]
	[InlineData(true, 1)]
	[InlineData(false, 0)]
	public void Render_TogglesRecipesNavigation(bool isRecipesEnabled, int expectedRecipeLinks)
	{
		using var context = CreateContext(isRecipesEnabled);
		context.ComponentFactories.AddStub<UserMenu>();

		var cut = context.Render<MainLayout>(parameters => parameters
			.Add(
				component => component.Body,
				static builder => builder.AddContent(0, "Body")));

		Assert.Equal(expectedRecipeLinks, cut.FindAll("a[href='/recipes']").Count);
	}

	private static BunitContext CreateContext(bool isRecipesEnabled)
	{
		var culture = CultureInfo.GetCultureInfo("nl-NL");
		CultureInfo.CurrentCulture = culture;
		CultureInfo.CurrentUICulture = culture;

		var context = new BunitContext();
		context.Services.AddLocalization(options => options.ResourcesPath = "Resources");
		context.Services.Configure<NutritionFeatureOptions>(options => options.Enabled = true);
		context.Services.Configure<RecipeFeatureOptions>(options => options.Enabled = isRecipesEnabled);
		return context;
	}
}