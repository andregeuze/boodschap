using Boodschap.Features.ShoppingLists.Domain;
using Boodschap.Shared.Realtime;

namespace Boodschap.Features.ShoppingLists.Application;

public sealed class ShoppingListService(
	IShoppingListRepository shoppingListRepository,
	StoreChangeNotifier storeChangeNotifier) : IShoppingListService
{
	public Task<IReadOnlyList<ShoppingList>> GetListsAsync(CancellationToken cancellationToken = default)
	{
		return shoppingListRepository.GetListsAsync(cancellationToken);
	}

	public Task<ShoppingList?> GetListAsync(int id, CancellationToken cancellationToken = default)
	{
		return shoppingListRepository.GetListAsync(id, cancellationToken);
	}

	public async Task<ShoppingList> CreateListAsync(string name, CancellationToken cancellationToken = default)
	{
		var shoppingList = await shoppingListRepository.CreateListAsync(name, cancellationToken);
		await storeChangeNotifier.NotifyChangedAsync(new StoreChange(shoppingList.Id));
		return shoppingList;
	}

	public Task<ShoppingList?> ArchiveListAsync(int listId, CancellationToken cancellationToken = default)
	{
		return ExecuteMutationAsync(
			() => shoppingListRepository.SetListArchivedStateAsync(listId, archived: true, cancellationToken),
			listId);
	}

	public Task<ShoppingList?> UnarchiveListAsync(int listId, CancellationToken cancellationToken = default)
	{
		return ExecuteMutationAsync(
			() => shoppingListRepository.SetListArchivedStateAsync(listId, archived: false, cancellationToken),
			listId);
	}

	public Task<ShoppingList?> AddItemAsync(int listId, string itemName, CancellationToken cancellationToken = default)
	{
		return ExecuteMutationAsync(() => shoppingListRepository.AddItemAsync(listId, itemName, cancellationToken), listId);
	}

	public Task<ShoppingList?> ToggleDoneAsync(int listId, int itemId, bool isDone, CancellationToken cancellationToken = default)
	{
		return ExecuteMutationAsync(() => shoppingListRepository.ToggleDoneAsync(listId, itemId, isDone, cancellationToken), listId);
	}

	public Task<ShoppingList?> RemoveItemAsync(int listId, int itemId, CancellationToken cancellationToken = default)
	{
		return ExecuteMutationAsync(() => shoppingListRepository.RemoveItemAsync(listId, itemId, cancellationToken), listId);
	}

	public Task<ShoppingList?> ReorderItemAsync(int listId, int itemId, int targetItemId, CancellationToken cancellationToken = default)
	{
		return ExecuteMutationAsync(() => shoppingListRepository.ReorderItemAsync(listId, itemId, targetItemId, cancellationToken), listId);
	}

	private async Task<ShoppingList?> ExecuteMutationAsync(Func<Task<MutationResult<ShoppingList>>> operation, int listId)
	{
		var result = await operation();
		if (result.Changed)
		{
			await storeChangeNotifier.NotifyChangedAsync(new StoreChange(listId));
		}

		return result.Value;
	}
}