using Boodschap.Features.ShoppingLists.Domain;

namespace Boodschap.Features.ShoppingLists.Application;

public interface IShoppingListRepository
{
	Task<IReadOnlyList<ShoppingList>> GetListsAsync(CancellationToken cancellationToken = default);
	Task<ShoppingList?> GetListAsync(int id, CancellationToken cancellationToken = default);
	Task<ShoppingList> CreateListAsync(string name, CancellationToken cancellationToken = default);
	Task<MutationResult<ShoppingList>> SetListArchivedStateAsync(int listId, bool archived, CancellationToken cancellationToken = default);
	Task<MutationResult<ShoppingList>> AddItemAsync(int listId, string itemName, CancellationToken cancellationToken = default);
	Task<MutationResult<ShoppingList>> ToggleDoneAsync(int listId, int itemId, bool isDone, CancellationToken cancellationToken = default);
	Task<MutationResult<ShoppingList>> RemoveItemAsync(int listId, int itemId, CancellationToken cancellationToken = default);
	Task<MutationResult<ShoppingList>> ReorderItemAsync(int listId, int itemId, int targetItemId, CancellationToken cancellationToken = default);
}