using Boodschap.Features.ShoppingLists.Application;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace Boodschap.Features.ShoppingLists.Presentation.Mcp;

[McpServerToolType]
public sealed class ShoppingListMcpTools(IShoppingListService shoppingListService)
{
	[McpServerTool(Name = "list_shopping_lists", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
	[Description("List all Boodschap shopping lists and their current items.")]
	public async Task<IReadOnlyList<ShoppingListResponse>> ListShoppingListsAsync(
		CancellationToken cancellationToken = default)
	{
		var lists = await shoppingListService.GetListsAsync(cancellationToken);
		return [.. lists.Select(ShoppingListApiMapper.ToResponse)];
	}

	[McpServerTool(Name = "create_shopping_list", ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = false)]
	[Description("Create a Boodschap shopping list, optionally populated with initial grocery items.")]
	public async Task<ShoppingListResponse> CreateShoppingListAsync(
		[Description("The name of the shopping list.")] string name,
		[Description("An optional description of the shopping list.")] string? description = null,
		[Description("Optional grocery item names to add to the new list.")] IReadOnlyList<string>? items = null,
		CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrWhiteSpace(name))
		{
			throw new ArgumentException("A shopping list name is required.", nameof(name));
		}

		var itemNames = items?
			.Select(item => item?.Trim())
			.Where(item => !string.IsNullOrWhiteSpace(item))
			.Cast<string>()
			.ToArray() ?? [];

		var shoppingList = await shoppingListService.CreateListAsync(name.Trim(), description?.Trim() ?? string.Empty, cancellationToken);
		foreach (var itemName in itemNames)
		{
			shoppingList = await shoppingListService.AddItemAsync(shoppingList.Id, itemName, cancellationToken)
				?? throw new InvalidOperationException("The new shopping list could not be populated.");
		}

		return shoppingList.ToResponse();
	}

	[McpServerTool(Name = "add_shopping_list_item", ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = false)]
	[Description("Add a grocery item to an existing Boodschap shopping list.")]
	public async Task<ShoppingListResponse> AddShoppingListItemAsync(
		[Description("The ID of the shopping list.")] int listId,
		[Description("The name of the grocery item to add.")] string itemName,
		CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrWhiteSpace(itemName))
		{
			throw new ArgumentException("A grocery item name is required.", nameof(itemName));
		}

		var shoppingList = await shoppingListService.AddItemAsync(listId, itemName.Trim(), cancellationToken)
			?? throw new KeyNotFoundException($"Shopping list {listId} was not found.");

		return shoppingList.ToResponse();
	}

	[McpServerTool(Name = "remove_shopping_list_item", ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = false)]
	[Description("Remove a grocery item from an existing Boodschap shopping list.")]
	public async Task<ShoppingListResponse> RemoveShoppingListItemAsync(
		[Description("The ID of the shopping list.")] int listId,
		[Description("The ID of the grocery item to remove.")] int itemId,
		CancellationToken cancellationToken = default)
	{
		var existingList = await shoppingListService.GetListAsync(listId, cancellationToken)
			?? throw new KeyNotFoundException($"Shopping list {listId} was not found.");
		if (existingList.Items.All(item => item.Id != itemId))
		{
			throw new KeyNotFoundException($"Shopping list item {itemId} was not found on shopping list {listId}.");
		}

		var shoppingList = await shoppingListService.RemoveItemAsync(listId, itemId, cancellationToken)
			?? throw new KeyNotFoundException($"Shopping list item {itemId} was not found on shopping list {listId}.");

		return shoppingList.ToResponse();
	}
}