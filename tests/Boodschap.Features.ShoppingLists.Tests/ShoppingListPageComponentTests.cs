using System.Globalization;
using AngleSharp.Dom;
using Bunit;
using Bunit.Rendering;
using Boodschap.Features.ShoppingLists.Application;
using Boodschap.Features.ShoppingLists.Domain;
using Boodschap.Features.ShoppingLists.Presentation.Pages;
using Boodschap.Features.ShoppingLists.Tests.Testing;
using Boodschap.Shared.Realtime;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Boodschap.Features.ShoppingLists.Tests;

public sealed class ShoppingListPageComponentTests
{
	[Fact]
	public void Render_ShowsAllItemsAndNoItemFilters()
	{
		var service = new FakeShoppingListService([CreateActiveList()]);

		using var context = CreateContext(service, new StoreChangeNotifier());
		var cut = context.Render<ShoppingListPage>(parameters => parameters.Add(page => page.Id, 1));

		cut.WaitForAssertion(() =>
		{
			Assert.Contains("Gebruik het bewerkicoon om te hernoemen. Sleep om de volgorde te wijzigen.", cut.Markup);
			Assert.Contains("Milk", cut.Markup);
			Assert.Contains("Coffee", cut.Markup);
			Assert.Contains("Eggs", cut.Markup);
			Assert.DoesNotContain("Boodschappen filteren", cut.Markup);
		});

		Assert.DoesNotContain(cut.FindAll("button"), button =>
			button.TextContent.Trim() is "Alles" or "Nodig" or "Gekocht");
	}

	[Fact]
	public void AddItem_ShowsNewItem()
	{
		var service = new FakeShoppingListService([CreateActiveList()]);

		using var context = CreateContext(service, new StoreChangeNotifier());
		var cut = context.Render<ShoppingListPage>(parameters => parameters.Add(page => page.Id, 1));

		cut.Find("button[aria-label='Nieuwe boodschap']").Click();
		cut.Find("input[placeholder='Boodschap toevoegen']").Input("Bananas");
		FindButton(cut, "Toevoegen").Click();

		cut.WaitForAssertion(() =>
		{
			Assert.Equal((1, "Bananas"), service.LastAddedItem);
			Assert.Contains("Bananas", cut.Markup);
			Assert.Contains("Milk", cut.Markup);
			Assert.Contains("Eggs", cut.Markup);
			Assert.Contains("Gebruik het bewerkicoon om te hernoemen. Sleep om de volgorde te wijzigen.", cut.Markup);
		});
	}

	[Fact]
	public void ArchiveList_NavigatesToArchivedOverview()
	{
		var service = new FakeShoppingListService([CreateActiveList()]);

		using var context = CreateContext(service, new StoreChangeNotifier());
		var navigation = context.Services.GetRequiredService<NavigationManager>();
		var cut = context.Render<ShoppingListPage>(parameters => parameters.Add(page => page.Id, 1));

		FindButton(cut, "Archiveren").Click();

		cut.WaitForAssertion(() =>
		{
			Assert.Equal(1, service.LastArchivedListId);
			Assert.Equal("http://localhost/?tab=Archived", navigation.Uri);
		});
	}

	[Fact]
	public void BackButton_UsesArchivedOverviewForArchivedLists()
	{
		var service = new FakeShoppingListService([CreateArchivedList()]);

		using var context = CreateContext(service, new StoreChangeNotifier());
		var navigation = context.Services.GetRequiredService<NavigationManager>();
		var cut = context.Render<ShoppingListPage>(parameters => parameters.Add(page => page.Id, 1));

		FindButton(cut, "Terug").Click();

		cut.WaitForAssertion(() => Assert.Equal("http://localhost/?tab=Archived", navigation.Uri));
	}

	private static BunitContext CreateContext(IShoppingListService shoppingLists, StoreChangeNotifier notifier)
	{
		var culture = CultureInfo.GetCultureInfo("nl-NL");
		CultureInfo.CurrentCulture = culture;
		CultureInfo.CurrentUICulture = culture;

		var context = new BunitContext();
		context.Services.AddLocalization(options => options.ResourcesPath = "Resources");
		context.Services.AddSingleton<IShoppingListService>(shoppingLists);
		context.Services.AddSingleton(notifier);
		return context;
	}

	private static IElement FindButton<TComponent>(IRenderedComponent<TComponent> renderedFragment, string text)
		where TComponent : IComponent
	{
		return renderedFragment.FindAll("button").Single(button => string.Equals(button.TextContent.Trim(), text, StringComparison.Ordinal));
	}

	private static ShoppingList CreateActiveList()
	{
		return new ShoppingList
		{
			Id = 1,
			Name = "Weekly groceries",
			Description = "Fresh produce, dairy, and pantry basics.",
			Archived = false,
			SortOrder = 0,
			Items =
			[
				new() { Id = 11, ShoppingListId = 1, Name = "Milk", SortOrder = 0 },
				new() { Id = 12, ShoppingListId = 1, Name = "Coffee", SortOrder = 1 },
				new() { Id = 13, ShoppingListId = 1, Name = "Eggs", IsDone = true, SortOrder = 2 }
			]
		};
	}

	private static ShoppingList CreateArchivedList()
	{
		var shoppingList = CreateActiveList();
		shoppingList.Name = "Camping weekend";
		shoppingList.Archived = true;
		foreach (var item in shoppingList.Items)
		{
			item.IsDone = true;
		}

		return shoppingList;
	}
}
