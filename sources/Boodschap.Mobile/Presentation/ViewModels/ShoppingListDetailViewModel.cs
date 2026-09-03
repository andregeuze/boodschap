using System.Windows.Input;
using System.Collections.ObjectModel;
using Boodschap.Features.ShoppingLists.Domain;
using Boodschap.Shared.Localization;
using Microsoft.Extensions.Localization;

namespace Boodschap.Mobile.Presentation.ViewModels;

public sealed class ShoppingListDetailViewModel : ObservableObject
{
	private readonly Func<ShoppingItemViewModel?, bool, Task> toggleDoneAsync;
	private readonly Func<ShoppingItemViewModel?, ShoppingItemViewModel?, Task> reorderItemAsync;
	private bool isVisible;
	private string heading = string.Empty;
	private string description = string.Empty;
	private string summary = string.Empty;
	private bool isEmptyVisible;

	public ShoppingListDetailViewModel(
		IStringLocalizer<AppStrings> localizer,
		Func<Task> goBackAsync,
		Func<Task> editCurrentListAsync,
		Func<Task> archiveCurrentListAsync,
		Func<Task> addItemAsync,
		Func<ShoppingItemViewModel?, bool, Task> toggleDoneAsync,
		Func<ShoppingItemViewModel?, Task> renameItemAsync,
		Func<ShoppingItemViewModel?, Task> removeItemAsync,
		Func<ShoppingItemViewModel?, ShoppingItemViewModel?, Task> reorderItemAsync)
	{
		this.toggleDoneAsync = toggleDoneAsync;
		this.reorderItemAsync = reorderItemAsync;
		BackText = localizer["Common.Back"].Value;
		EditText = localizer["Common.Edit"].Value;
		ArchiveText = localizer["Common.Archive"].Value;
		AddItemText = localizer["Shopping.NewItem"].Value;
		ItemHint = localizer["Shopping.ItemInteractionHintWithReorder"].Value;
		EmptyText = localizer["Shopping.EmptyState"].Value;
		RemoveText = localizer["Common.Remove"].Value;
		RenameText = localizer["Common.Edit"].Value;

		BackCommand = new Command(async () => await goBackAsync());
		EditCurrentListCommand = new Command(async () => await editCurrentListAsync());
		ArchiveCurrentListCommand = new Command(async () => await archiveCurrentListAsync());
		AddItemCommand = new Command(async () => await addItemAsync());
		RenameItemCommand = new Command<ShoppingItemViewModel?>(async item => await renameItemAsync(item));
		RemoveItemCommand = new Command<ShoppingItemViewModel?>(async item => await removeItemAsync(item));
	}

	public ObservableCollection<ShoppingItemViewModel> Items { get; } = [];

	public bool IsVisible
	{
		get => isVisible;
		set => SetProperty(ref isVisible, value);
	}

	public string Heading
	{
		get => heading;
		set => SetProperty(ref heading, value);
	}

	public string Description
	{
		get => description;
		set => SetProperty(ref description, value);
	}

	public string Summary
	{
		get => summary;
		set => SetProperty(ref summary, value);
	}

	public bool IsEmptyVisible
	{
		get => isEmptyVisible;
		set => SetProperty(ref isEmptyVisible, value);
	}

	public string BackText { get; }

	public string EditText { get; }

	public string ArchiveText { get; }

	public string AddItemText { get; }

	public string ItemHint { get; }

	public string EmptyText { get; }

	public string RemoveText { get; }

	public string RenameText { get; }

	public ICommand BackCommand { get; }

	public ICommand EditCurrentListCommand { get; }

	public ICommand ArchiveCurrentListCommand { get; }

	public ICommand AddItemCommand { get; }

	public ICommand RenameItemCommand { get; }

	public ICommand RemoveItemCommand { get; }

	public Task ToggleDoneAsync(ShoppingItemViewModel? item, bool isDone)
	{
		return toggleDoneAsync(item, isDone);
	}

	public Task ReorderItemAsync(ShoppingItemViewModel? item, ShoppingItemViewModel? targetItem)
	{
		return reorderItemAsync(item, targetItem);
	}

	public void SetList(ShoppingList list, IReadOnlyList<ShoppingItemViewModel> items, string summaryText)
	{
		Heading = list.Name;
		Description = list.Description;
		Summary = summaryText;
		ReplaceCollection(Items, items);
		IsEmptyVisible = items.Count == 0;
	}

	public void ClearList()
	{
		Heading = string.Empty;
		Description = string.Empty;
		Summary = string.Empty;
		Items.Clear();
		IsEmptyVisible = false;
	}

	private static void ReplaceCollection<T>(ObservableCollection<T> target, IEnumerable<T> items)
	{
		target.Clear();
		foreach (var item in items)
		{
			target.Add(item);
		}
	}
}