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

public sealed class HomePageComponentTests
{
	[Fact]
	public void Render_ShowsActiveListsByDefaultAndArchivedAfterTabChange()
	{
		var service = new FakeShoppingListService(
		[
			CreateList(1, "Weekly groceries", archived: false),
			CreateList(2, "Dinner party", archived: false),
			CreateList(3, "Camping weekend", archived: true)
		]);

		using var context = CreateContext(service, new StoreChangeNotifier());
		var cut = context.Render<Home>();

		cut.WaitForAssertion(() =>
		{
			Assert.Contains("Weekly groceries", cut.Markup);
			Assert.Contains("Dinner party", cut.Markup);
			Assert.DoesNotContain("Camping weekend", cut.Markup);
		});

		FindButton(cut, "Archived").Click();

		cut.WaitForAssertion(() =>
		{
			Assert.DoesNotContain("Weekly groceries", cut.Markup);
			Assert.DoesNotContain("Dinner party", cut.Markup);
			Assert.Contains("Camping weekend", cut.Markup);
		});
	}

	[Fact]
	public void CreateList_NavigatesToCreatedList()
	{
		var service = new FakeShoppingListService();

		using var context = CreateContext(service, new StoreChangeNotifier());
		var navigation = context.Services.GetRequiredService<NavigationManager>();
		var cut = context.Render<Home>();

		cut.Find("#new-list-name").Input("Weekend groceries");
		cut.Find("form").Submit();

		cut.WaitForAssertion(() =>
		{
			Assert.Equal("Weekend groceries", service.LastCreatedListName);
			Assert.Equal("http://localhost/lists/1", navigation.Uri);
		});
	}

	[Fact]
	public void RemoveArchivedList_RemovesCardWhenServiceSucceeds()
	{
		var service = new FakeShoppingListService([CreateList(7, "Camping weekend", archived: true)]);

		using var context = CreateContext(service, new StoreChangeNotifier());
		var navigation = context.Services.GetRequiredService<NavigationManager>();
		navigation.NavigateTo(navigation.GetUriWithQueryParameter("tab", "Archived"));
		var cut = context.Render<Home>();

		cut.WaitForAssertion(() => Assert.Contains("Camping weekend", cut.Markup));

		FindButton(cut, "Remove").Click();

		cut.WaitForAssertion(() =>
		{
			Assert.Equal(7, service.LastRemovedArchivedListId);
			Assert.DoesNotContain("Camping weekend", cut.Markup);
			Assert.Contains("No lists in this view yet.", cut.Markup);
		});
	}

	[Fact]
	public async Task StoreChange_RefreshesVisibleLists()
	{
		var notifier = new StoreChangeNotifier();
		var service = new FakeShoppingListService([CreateList(1, "Weekly groceries", archived: false)]);

		using var context = CreateContext(service, notifier);
		var cut = context.Render<Home>();

		cut.WaitForAssertion(() => Assert.DoesNotContain("Dinner party", cut.Markup));

		service.ReplaceLists(
			CreateList(1, "Weekly groceries", archived: false),
			CreateList(2, "Dinner party", archived: false));

		await notifier.NotifyChangedAsync(new StoreChange(null));

		cut.WaitForAssertion(() => Assert.Contains("Dinner party", cut.Markup));
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

	private static ShoppingList CreateList(int id, string name, bool archived)
	{
		return new ShoppingList
		{
			Id = id,
			Name = name,
			Description = $"{name} description",
			Archived = archived,
			SortOrder = id,
			Items =
			[
				new() { Id = (id * 10) + 1, ShoppingListId = id, Name = "Milk", SortOrder = 0 },
				new() { Id = (id * 10) + 2, ShoppingListId = id, Name = "Eggs", IsDone = true, SortOrder = 1 }
			]
		};
	}
}
