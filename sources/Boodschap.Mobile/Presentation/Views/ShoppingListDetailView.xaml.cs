using Boodschap.Mobile.Presentation.ViewModels;

namespace Boodschap.Mobile.Presentation.Views;

public partial class ShoppingListDetailView : ContentView
{
	public ShoppingListDetailView()
	{
		InitializeComponent();
	}

	private async void HandleItemCheckedChanged(object? sender, CheckedChangedEventArgs e)
	{
		if (BindingContext is not ShoppingListDetailViewModel viewModel ||
			(sender as BindableObject)?.BindingContext is not ShoppingItemViewModel item ||
			item.IsDone == e.Value)
		{
			return;
		}

		await viewModel.ToggleDoneAsync(item, e.Value);
	}

	private void HandleItemDragStarting(object? sender, DragStartingEventArgs e)
	{
		if ((sender as BindableObject)?.BindingContext is not ShoppingItemViewModel item)
		{
			e.Cancel = true;
			return;
		}

		e.Data.Properties[nameof(ShoppingItemViewModel)] = item;
	}

	private async void HandleItemDropped(object? sender, DropEventArgs e)
	{
		if (BindingContext is not ShoppingListDetailViewModel viewModel ||
			(sender as BindableObject)?.BindingContext is not ShoppingItemViewModel targetItem ||
			!e.Data.Properties.TryGetValue(nameof(ShoppingItemViewModel), out var draggedValue) ||
			draggedValue is not ShoppingItemViewModel draggedItem ||
			draggedItem.Item.Id == targetItem.Item.Id)
		{
			return;
		}

		await viewModel.ReorderItemAsync(draggedItem, targetItem);
	}
}