namespace Boodschap.Features.ShoppingLists.Application;

public sealed record ShoppingListResponse(
	int Id,
	string Name,
	string Description,
	bool Archived,
	int SortOrder,
	DateTime UpdatedAt,
	IReadOnlyList<ShoppingListItemResponse> Items);

public sealed record ShoppingListItemResponse(
	int Id,
	int ShoppingListId,
	string Name,
	bool IsDone,
	int SortOrder);

public sealed record CreateShoppingListRequest(string Name, string Description);

public sealed record UpdateShoppingListRequest(string Name, string Description);

public sealed record AddShoppingListItemRequest(string Name);

public sealed record RenameShoppingListItemRequest(string Name);

public sealed record ToggleShoppingListItemRequest(bool IsDone);

public sealed record ReorderShoppingListItemRequest(int TargetItemId);

public sealed record StoreChangedMessage(int? ListId);

public static class StoreChangeRealtimeDefaults
{
	public const string HubRoute = "/hubs/store-changes";
}