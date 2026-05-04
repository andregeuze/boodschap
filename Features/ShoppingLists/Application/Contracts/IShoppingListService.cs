using Boodschap.Features.ShoppingLists.Domain;

namespace Boodschap.Features.ShoppingLists.Application;

public interface IShoppingListService
{
	Task<IReadOnlyList<ShoppingList>> GetListsAsync(CancellationToken cancellationToken = default);
	Task<ShoppingList?> GetListAsync(int id, CancellationToken cancellationToken = default);
	Task<ShoppingList> CreateListAsync(string name, CancellationToken cancellationToken = default);
	Task<ShoppingList?> ArchiveListAsync(int listId, CancellationToken cancellationToken = default);
	Task<ShoppingList?> UnarchiveListAsync(int listId, CancellationToken cancellationToken = default);
	Task<bool> RemoveArchivedListAsync(int listId, CancellationToken cancellationToken = default);
	Task<ShoppingList?> AddItemAsync(int listId, string itemName, CancellationToken cancellationToken = default);
	Task<ShoppingList?> ToggleDoneAsync(int listId, int itemId, bool isDone, CancellationToken cancellationToken = default);
	Task<ShoppingList?> RemoveItemAsync(int listId, int itemId, CancellationToken cancellationToken = default);
	Task<ShoppingList?> ReorderItemAsync(int listId, int itemId, int targetItemId, CancellationToken cancellationToken = default);
}