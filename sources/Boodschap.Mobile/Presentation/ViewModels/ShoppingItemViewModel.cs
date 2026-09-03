using Boodschap.Features.ShoppingLists.Domain;

namespace Boodschap.Mobile.Presentation.ViewModels;

public sealed class ShoppingItemViewModel(ShoppingListItem item)
{
	public ShoppingListItem Item { get; } = item;

	public string Name => Item.Name;

	public bool IsDone => Item.IsDone;
}