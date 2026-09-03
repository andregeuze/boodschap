using Boodschap.Features.ShoppingLists.Domain;

namespace Boodschap.Mobile.Presentation.ViewModels;

public sealed class ShoppingItemViewModel(ShoppingListItem item) : ObservableObject
{
	public ShoppingListItem Item { get; } = item;

	public string Name => Item.Name;

	public bool IsDone => Item.IsDone;

	public void SetIsDone(bool isDone)
	{
		if (Item.IsDone == isDone)
		{
			return;
		}

		Item.IsDone = isDone;
		OnPropertyChanged(nameof(IsDone));
	}
}