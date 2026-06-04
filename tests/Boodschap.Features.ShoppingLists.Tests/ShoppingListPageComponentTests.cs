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
	public void SwitchingFilters_UpdatesVisibleItemsAndInteractionHint()
	{
		var service = new FakeShoppingListService([CreateActiveList()]);

		using var context = CreateContext(service, new StoreChangeNotifier());
		var cut = context.Render<ShoppingListPage>(parameters => parameters.Add(page => page.Id, 1));

		cut.WaitForAssertion(() =>
		{
			Assert.Contains("Use the edit icon to rename. Drag to reorder.", cut.Markup);
			Assert.Contains("Milk", cut.Markup);
			Assert.Contains("Coffee", cut.Markup);
			Assert.Contains("Eggs", cut.Markup);
		});

		FindButton(cut, "Needed").Click();

		cut.WaitForAssertion(() =>
		{
			Assert.Contains("Use the edit icon to rename.", cut.Markup);
			Assert.DoesNotContain("Drag to reorder.", cut.Markup);
			Assert.Contains("Milk", cut.Markup);
			Assert.Contains("Coffee", cut.Markup);
			Assert.DoesNotContain("Eggs", cut.Markup);
		});

		FindButton(cut, "Purchased").Click();

		cut.WaitForAssertion(() =>
		{
			Assert.Contains("Eggs", cut.Markup);
			Assert.DoesNotContain("Milk", cut.Markup);
			Assert.DoesNotContain("Coffee", cut.Markup);
		});
	}

	[Fact]
	public void AddItem_ResetsFilterToAllAndShowsNewItem()
	{
		var service = new FakeShoppingListService([CreateActiveList()]);

		using var context = CreateContext(service, new StoreChangeNotifier());
		var cut = context.Render<ShoppingListPage>(parameters => parameters.Add(page => page.Id, 1));

		FindButton(cut, "Purchased").Click();
		FindButton(cut, "New item").Click();
		cut.Find("input[placeholder='Add grocery item']").Input("Bananas");
		FindButton(cut, "Add").Click();

		cut.WaitForAssertion(() =>
		{
			Assert.Equal((1, "Bananas"), service.LastAddedItem);
			Assert.Contains("Bananas", cut.Markup);
			Assert.Contains("Milk", cut.Markup);
			Assert.Contains("Eggs", cut.Markup);
			Assert.Contains("Use the edit icon to rename. Drag to reorder.", cut.Markup);
		});
	}

	[Fact]
	public void ChangingFilters_CancelsInlineRename()
	{
		var service = new FakeShoppingListService([CreateActiveList()]);

		using var context = CreateContext(service, new StoreChangeNotifier());
		var cut = context.Render<ShoppingListPage>(parameters => parameters.Add(page => page.Id, 1));

		cut.Find("button[aria-label='Edit Coffee']").Click();
		cut.WaitForAssertion(() => Assert.Single(cut.FindAll("input[placeholder='Rename item']")));

		FindButton(cut, "Needed").Click();

		cut.WaitForAssertion(() =>
		{
			Assert.Empty(cut.FindAll("input[placeholder='Rename item']"));
			Assert.Single(cut.FindAll("button[aria-label='Edit Coffee']"));
		});
	}

	[Fact]
	public void ArchiveList_NavigatesToArchivedOverview()
	{
		var service = new FakeShoppingListService([CreateActiveList()]);

		using var context = CreateContext(service, new StoreChangeNotifier());
		var navigation = context.Services.GetRequiredService<NavigationManager>();
		var cut = context.Render<ShoppingListPage>(parameters => parameters.Add(page => page.Id, 1));

		FindButton(cut, "Archive").Click();

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

		FindButton(cut, "Back").Click();

		cut.WaitForAssertion(() => Assert.Equal("http://localhost/?tab=Archived", navigation.Uri));
	}

	private static BunitContext CreateContext(IShoppingListService shoppingLists, StoreChangeNotifier notifier)
	{
		var context = new BunitContext();
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
