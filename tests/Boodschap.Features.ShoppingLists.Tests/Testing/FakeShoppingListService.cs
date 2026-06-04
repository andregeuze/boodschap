using Boodschap.Features.ShoppingLists.Application;
using Boodschap.Features.ShoppingLists.Domain;

namespace Boodschap.Features.ShoppingLists.Tests.Testing;

public sealed class FakeShoppingListService(IEnumerable<ShoppingList>? shoppingLists = null) : IShoppingListService
{
	private readonly List<ShoppingList> storedLists = shoppingLists?.Select(CloneList).ToList() ?? [];
	private int nextListId = (shoppingLists?.Select(list => list.Id).DefaultIfEmpty(0).Max() ?? 0) + 1;
	private int nextItemId = (shoppingLists?.SelectMany(list => list.Items).Select(item => item.Id).DefaultIfEmpty(0).Max() ?? 0) + 1;

	public string? LastCreatedListName { get; private set; }
	public int? LastArchivedListId { get; private set; }
	public int? LastUnarchivedListId { get; private set; }
	public int? LastRemovedArchivedListId { get; private set; }
	public (int ListId, string ItemName)? LastAddedItem { get; private set; }
	public (int ListId, int ItemId, string Name)? LastRenamedItem { get; private set; }
	public (int ListId, int ItemId, bool IsDone)? LastToggleDone { get; private set; }
	public (int ListId, int ItemId)? LastRemovedItem { get; private set; }
	public (int ListId, int ItemId, int TargetItemId)? LastReorderedItem { get; private set; }
	public bool RemoveArchivedListSucceeds { get; set; } = true;

	public void ReplaceLists(params ShoppingList[] shoppingLists)
	{
		storedLists.Clear();
		storedLists.AddRange(shoppingLists.Select(CloneList));
		nextListId = storedLists.Select(list => list.Id).DefaultIfEmpty(0).Max() + 1;
		nextItemId = storedLists.SelectMany(list => list.Items).Select(item => item.Id).DefaultIfEmpty(0).Max() + 1;
	}

	public Task<IReadOnlyList<ShoppingList>> GetListsAsync(CancellationToken cancellationToken = default)
	{
		return Task.FromResult<IReadOnlyList<ShoppingList>>([.. storedLists.OrderBy(list => list.SortOrder).Select(CloneList)]);
	}

	public Task<ShoppingList?> GetListAsync(int id, CancellationToken cancellationToken = default)
	{
		return Task.FromResult(storedLists.SingleOrDefault(list => list.Id == id) is { } shoppingList ? CloneList(shoppingList) : null);
	}

	public Task<ShoppingList> CreateListAsync(string name, CancellationToken cancellationToken = default)
	{
		var normalizedName = name.Trim();
		LastCreatedListName = normalizedName;

		var shoppingList = new ShoppingList
		{
			Id = nextListId++,
			Name = normalizedName,
			Description = "A fresh list ready for new items.",
			Archived = false,
			SortOrder = storedLists.Count == 0 ? 0 : storedLists.Min(list => list.SortOrder) - 1,
			Items = []
		};

		storedLists.Add(shoppingList);
		return Task.FromResult(CloneList(shoppingList));
	}

	public Task<ShoppingList?> RenameListAsync(int listId, string name, CancellationToken cancellationToken = default)
	{
		var shoppingList = storedLists.SingleOrDefault(list => list.Id == listId);
		if (shoppingList is null)
		{
			return Task.FromResult<ShoppingList?>(null);
		}

		var normalizedName = name.Trim();
		if (!string.IsNullOrWhiteSpace(normalizedName))
		{
			shoppingList.Name = normalizedName;
		}

		return Task.FromResult<ShoppingList?>(CloneList(shoppingList));
	}

	public Task<ShoppingList?> ArchiveListAsync(int listId, CancellationToken cancellationToken = default)
	{
		LastArchivedListId = listId;
		return Task.FromResult(SetArchivedState(listId, archived: true));
	}

	public Task<ShoppingList?> UnarchiveListAsync(int listId, CancellationToken cancellationToken = default)
	{
		LastUnarchivedListId = listId;
		return Task.FromResult(SetArchivedState(listId, archived: false));
	}

	public Task<bool> RemoveArchivedListAsync(int listId, CancellationToken cancellationToken = default)
	{
		LastRemovedArchivedListId = listId;
		var shoppingList = storedLists.SingleOrDefault(list => list.Id == listId);
		if (shoppingList is null || !shoppingList.Archived || !RemoveArchivedListSucceeds)
		{
			return Task.FromResult(false);
		}

		storedLists.Remove(shoppingList);
		return Task.FromResult(true);
	}

	public Task<ShoppingList?> AddItemAsync(int listId, string itemName, CancellationToken cancellationToken = default)
	{
		var normalizedName = itemName.Trim();
		LastAddedItem = (listId, normalizedName);

		var shoppingList = storedLists.SingleOrDefault(list => list.Id == listId);
		if (shoppingList is null || string.IsNullOrWhiteSpace(normalizedName))
		{
			return Task.FromResult(shoppingList is null ? null : CloneList(shoppingList));
		}

		var insertIndex = shoppingList.Items.FindIndex(item => item.IsDone);
		if (insertIndex < 0)
		{
			insertIndex = shoppingList.Items.Count;
		}

		shoppingList.Items.Insert(insertIndex, new ShoppingListItem
		{
			Id = nextItemId++,
			ShoppingListId = listId,
			Name = normalizedName,
			SortOrder = insertIndex
		});

		NormalizeItemSortOrder(shoppingList);
		return Task.FromResult<ShoppingList?>(CloneList(shoppingList));
	}

	public Task<ShoppingList?> RenameItemAsync(int listId, int itemId, string name, CancellationToken cancellationToken = default)
	{
		LastRenamedItem = (listId, itemId, name);
		var shoppingList = storedLists.SingleOrDefault(list => list.Id == listId);
		var item = shoppingList?.Items.SingleOrDefault(entry => entry.Id == itemId);
		if (shoppingList is null || item is null || string.IsNullOrWhiteSpace(name))
		{
			return Task.FromResult(shoppingList is null ? null : CloneList(shoppingList));
		}

		item.Name = name.Trim();
		return Task.FromResult<ShoppingList?>(CloneList(shoppingList));
	}

	public Task<ShoppingList?> ToggleDoneAsync(int listId, int itemId, bool isDone, CancellationToken cancellationToken = default)
	{
		LastToggleDone = (listId, itemId, isDone);
		var shoppingList = storedLists.SingleOrDefault(list => list.Id == listId);
		var item = shoppingList?.Items.SingleOrDefault(entry => entry.Id == itemId);
		if (shoppingList is null || item is null)
		{
			return Task.FromResult(shoppingList is null ? null : CloneList(shoppingList));
		}

		if (item.IsDone == isDone)
		{
			return Task.FromResult<ShoppingList?>(CloneList(shoppingList));
		}

		item.IsDone = isDone;
		shoppingList.Items.Remove(item);
		if (isDone)
		{
			shoppingList.Items.Add(item);
		}
		else
		{
			var firstDoneIndex = shoppingList.Items.FindIndex(entry => entry.IsDone);
			shoppingList.Items.Insert(firstDoneIndex < 0 ? shoppingList.Items.Count : firstDoneIndex, item);
		}

		NormalizeItemSortOrder(shoppingList);
		return Task.FromResult<ShoppingList?>(CloneList(shoppingList));
	}

	public Task<ShoppingList?> RemoveItemAsync(int listId, int itemId, CancellationToken cancellationToken = default)
	{
		LastRemovedItem = (listId, itemId);
		var shoppingList = storedLists.SingleOrDefault(list => list.Id == listId);
		var item = shoppingList?.Items.SingleOrDefault(entry => entry.Id == itemId);
		if (shoppingList is null || item is null)
		{
			return Task.FromResult(shoppingList is null ? null : CloneList(shoppingList));
		}

		shoppingList.Items.Remove(item);
		NormalizeItemSortOrder(shoppingList);
		return Task.FromResult<ShoppingList?>(CloneList(shoppingList));
	}

	public Task<ShoppingList?> ReorderItemAsync(int listId, int itemId, int targetItemId, CancellationToken cancellationToken = default)
	{
		LastReorderedItem = (listId, itemId, targetItemId);
		var shoppingList = storedLists.SingleOrDefault(list => list.Id == listId);
		if (shoppingList is null)
		{
			return Task.FromResult<ShoppingList?>(null);
		}

		var draggedItem = shoppingList.Items.SingleOrDefault(item => item.Id == itemId);
		var targetItem = shoppingList.Items.SingleOrDefault(item => item.Id == targetItemId);
		if (draggedItem is null || targetItem is null || draggedItem == targetItem)
		{
			return Task.FromResult<ShoppingList?>(CloneList(shoppingList));
		}

		shoppingList.Items.Remove(draggedItem);
		shoppingList.Items.Insert(shoppingList.Items.IndexOf(targetItem), draggedItem);
		NormalizeItemSortOrder(shoppingList);
		return Task.FromResult<ShoppingList?>(CloneList(shoppingList));
	}

	private ShoppingList? SetArchivedState(int listId, bool archived)
	{
		var shoppingList = storedLists.SingleOrDefault(list => list.Id == listId);
		if (shoppingList is null)
		{
			return null;
		}

		shoppingList.Archived = archived;
		return CloneList(shoppingList);
	}

	private static void NormalizeItemSortOrder(ShoppingList shoppingList)
	{
		for (var index = 0; index < shoppingList.Items.Count; index++)
		{
			shoppingList.Items[index].SortOrder = index;
		}
	}

	private static ShoppingList CloneList(ShoppingList shoppingList)
	{
		return new ShoppingList
		{
			Id = shoppingList.Id,
			Name = shoppingList.Name,
			Description = shoppingList.Description,
			Archived = shoppingList.Archived,
			SortOrder = shoppingList.SortOrder,
			Items = shoppingList.Items
				.OrderBy(item => item.SortOrder)
				.Select(item => new ShoppingListItem
				{
					Id = item.Id,
					ShoppingListId = item.ShoppingListId,
					Name = item.Name,
					IsDone = item.IsDone,
					SortOrder = item.SortOrder
				})
				.ToList()
		};
	}
}
