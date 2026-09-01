using Boodschap.Features.ShoppingLists.Application;
using Boodschap.Features.ShoppingLists.Domain;

namespace Boodschap.Features.ShoppingLists.Presentation;

public static class ShoppingListApiMapper
{
	public static ShoppingListResponse ToResponse(this ShoppingList shoppingList)
	{
		return new ShoppingListResponse(
			shoppingList.Id,
			shoppingList.Name,
			shoppingList.Description,
			shoppingList.Archived,
			shoppingList.SortOrder,
			shoppingList.UpdatedAt,
			[.. shoppingList.Items.Select(ToResponse)]);
	}

	public static ShoppingListItemResponse ToResponse(this ShoppingListItem item)
	{
		return new ShoppingListItemResponse(
			item.Id,
			item.ShoppingListId,
			item.Name,
			item.IsDone,
			item.SortOrder);
	}
}