using Boodschap.Features.ShoppingLists.Domain;
using Boodschap.Features.ShoppingLists.Presentation.Mcp;
using Boodschap.Features.ShoppingLists.Tests.Testing;

namespace Boodschap.Features.ShoppingLists.Tests;

public sealed class ShoppingListMcpToolsTests
{
	[Fact]
	public async Task ListShoppingListsAsync_ReturnsTransportResponses()
	{
		var service = new FakeShoppingListService(
		[
			new ShoppingList { Id = 7, Name = "Weekend", Description = "Breakfast", Items = [] }
		]);
		var tools = new ShoppingListMcpTools(service);

		var result = await tools.ListShoppingListsAsync();

		var shoppingList = Assert.Single(result);
		Assert.Equal(7, shoppingList.Id);
		Assert.Equal("Weekend", shoppingList.Name);
		Assert.Equal("Breakfast", shoppingList.Description);
	}

	[Fact]
	public async Task CreateShoppingListAsync_CreatesListWithNormalizedInitialItems()
	{
		var service = new FakeShoppingListService();
		var tools = new ShoppingListMcpTools(service);

		var result = await tools.CreateShoppingListAsync(
			" Weekend ",
			" Breakfast ",
			[" Milk ", " ", "Eggs"]);

		Assert.Equal("Weekend", service.LastCreatedListName);
		Assert.Equal("Breakfast", service.LastCreatedListDescription);
		Assert.Equal(["Milk", "Eggs"], result.Items.Select(item => item.Name));
	}

	[Fact]
	public async Task CreateShoppingListAsync_WithBlankName_ThrowsBeforeCreatingList()
	{
		var service = new FakeShoppingListService();
		var tools = new ShoppingListMcpTools(service);

		await Assert.ThrowsAsync<ArgumentException>(() => tools.CreateShoppingListAsync(" "));

		Assert.Null(service.LastCreatedListName);
	}

	[Fact]
	public async Task AddShoppingListItemAsync_AddsNormalizedItemAndReturnsUpdatedList()
	{
		var service = new FakeShoppingListService(
		[
			new ShoppingList { Id = 7, Name = "Weekend", Items = [] }
		]);
		var tools = new ShoppingListMcpTools(service);

		var result = await tools.AddShoppingListItemAsync(7, " Bananas ");

		Assert.Equal((7, "Bananas"), service.LastAddedItem);
		Assert.Equal("Bananas", Assert.Single(result.Items).Name);
	}

	[Fact]
	public async Task AddShoppingListItemAsync_WithBlankName_ThrowsBeforeAddingItem()
	{
		var service = new FakeShoppingListService();
		var tools = new ShoppingListMcpTools(service);

		await Assert.ThrowsAsync<ArgumentException>(() => tools.AddShoppingListItemAsync(7, " "));

		Assert.Null(service.LastAddedItem);
	}

	[Fact]
	public async Task AddShoppingListItemAsync_WithUnknownList_ThrowsKeyNotFoundException()
	{
		var service = new FakeShoppingListService();
		var tools = new ShoppingListMcpTools(service);

		var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
			() => tools.AddShoppingListItemAsync(404, "Bananas"));

		Assert.Equal("Shopping list 404 was not found.", exception.Message);
	}

	[Fact]
	public async Task RemoveShoppingListItemAsync_RemovesItemAndReturnsUpdatedList()
	{
		var service = new FakeShoppingListService(
		[
			new ShoppingList
			{
				Id = 7,
				Name = "Weekend",
				Items =
				[
					new ShoppingListItem { Id = 11, ShoppingListId = 7, Name = "Bananas" },
					new ShoppingListItem { Id = 12, ShoppingListId = 7, Name = "Milk" }
				]
			}
		]);
		var tools = new ShoppingListMcpTools(service);

		var result = await tools.RemoveShoppingListItemAsync(7, 11);

		Assert.Equal((7, 11), service.LastRemovedItem);
		Assert.Equal("Milk", Assert.Single(result.Items).Name);
	}

	[Fact]
	public async Task RemoveShoppingListItemAsync_WithUnknownItem_ThrowsBeforeRemovingItem()
	{
		var service = new FakeShoppingListService(
		[
			new ShoppingList { Id = 7, Name = "Weekend", Items = [] }
		]);
		var tools = new ShoppingListMcpTools(service);

		var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
			() => tools.RemoveShoppingListItemAsync(7, 404));

		Assert.Equal("Shopping list item 404 was not found on shopping list 7.", exception.Message);
		Assert.Null(service.LastRemovedItem);
	}

	[Fact]
	public async Task RemoveShoppingListItemAsync_WithUnknownList_ThrowsBeforeRemovingItem()
	{
		var service = new FakeShoppingListService();
		var tools = new ShoppingListMcpTools(service);

		var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
			() => tools.RemoveShoppingListItemAsync(404, 11));

		Assert.Equal("Shopping list 404 was not found.", exception.Message);
		Assert.Null(service.LastRemovedItem);
	}
}