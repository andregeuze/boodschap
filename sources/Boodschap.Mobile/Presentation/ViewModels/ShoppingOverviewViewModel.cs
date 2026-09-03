using System.Windows.Input;
using System.Collections.ObjectModel;
using Boodschap.Features.ShoppingLists.Domain;
using Boodschap.Shared.Localization;
using Microsoft.Extensions.Localization;

namespace Boodschap.Mobile.Presentation.ViewModels;

public sealed class ShoppingOverviewViewModel : ObservableObject
{
	private bool isVisible;
	private bool isActiveEmptyVisible;
	private bool isArchivedHeadingVisible;
	private readonly string listSummaryFormat;

	public ShoppingOverviewViewModel(
		IStringLocalizer<AppStrings> localizer,
		Func<Task> createListAsync,
		Func<ShoppingListCardViewModel?, Task> openListAsync,
		Func<ShoppingListCardViewModel?, Task> editListAsync,
		Func<ShoppingListCardViewModel?, Task> archiveListAsync,
		Func<ShoppingListCardViewModel?, Task> unarchiveListAsync,
		Func<ShoppingListCardViewModel?, Task> removeArchivedListAsync)
	{
		Eyebrow = localizer["Shopping.HomeEyebrow"].Value;
		Description = localizer["Shopping.HomeDescription"].Value;
		CreateListText = localizer["Shopping.AddNewList"].Value;
		EditText = localizer["Common.Edit"].Value;
		ArchiveText = localizer["Common.Archive"].Value;
		UnarchiveText = localizer["Common.Unarchive"].Value;
		RemoveText = localizer["Common.Remove"].Value;
		ArchivedHeading = localizer["Common.Archived"].Value;
		ActiveEmptyText = localizer["Shopping.EmptyState"].Value;
		listSummaryFormat = localizer["Shopping.ListSummary"].Value;

		CreateListCommand = new Command(async () => await createListAsync());
		OpenListCommand = new Command<ShoppingListCardViewModel?>(async list => await openListAsync(list));
		EditListCommand = new Command<ShoppingListCardViewModel?>(async list => await editListAsync(list));
		ArchiveListCommand = new Command<ShoppingListCardViewModel?>(async list => await archiveListAsync(list));
		UnarchiveListCommand = new Command<ShoppingListCardViewModel?>(async list => await unarchiveListAsync(list));
		RemoveArchivedListCommand = new Command<ShoppingListCardViewModel?>(async list => await removeArchivedListAsync(list));
	}

	public ObservableCollection<ShoppingListCardViewModel> ActiveLists { get; } = [];

	public ObservableCollection<ShoppingListCardViewModel> ArchivedLists { get; } = [];

	public bool IsVisible
	{
		get => isVisible;
		set => SetProperty(ref isVisible, value);
	}

	public bool IsActiveEmptyVisible
	{
		get => isActiveEmptyVisible;
		set => SetProperty(ref isActiveEmptyVisible, value);
	}

	public bool IsArchivedHeadingVisible
	{
		get => isArchivedHeadingVisible;
		set => SetProperty(ref isArchivedHeadingVisible, value);
	}

	public string Eyebrow { get; }

	public string Description { get; }

	public string CreateListText { get; }

	public string EditText { get; }

	public string ArchiveText { get; }

	public string UnarchiveText { get; }

	public string RemoveText { get; }

	public string ArchivedHeading { get; }

	public string ActiveEmptyText { get; }

	public ICommand CreateListCommand { get; }

	public ICommand OpenListCommand { get; }

	public ICommand EditListCommand { get; }

	public ICommand ArchiveListCommand { get; }

	public ICommand UnarchiveListCommand { get; }

	public ICommand RemoveArchivedListCommand { get; }

	public void SetLists(IReadOnlyList<ShoppingList> lists)
	{
		var cards = lists.Select(list => new ShoppingListCardViewModel(list, listSummaryFormat)).ToList();
		ReplaceCollection(ActiveLists, cards.Where(card => !card.Archived));
		ReplaceCollection(ArchivedLists, cards.Where(card => card.Archived));

		IsActiveEmptyVisible = ActiveLists.Count == 0;
		IsArchivedHeadingVisible = ArchivedLists.Count > 0;
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