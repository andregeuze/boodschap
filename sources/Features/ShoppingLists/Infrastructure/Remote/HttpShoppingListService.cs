using Boodschap.Features.ShoppingLists.Application;
using Boodschap.Features.ShoppingLists.Domain;
using System.Net;
using System.Net.Http.Json;

namespace Boodschap.Features.ShoppingLists.Infrastructure.Remote;

public sealed class HttpShoppingListService(HttpClient httpClient) : IShoppingListService
{
	private const string BasePath = "api/shopping-lists";

	public async Task<IReadOnlyList<ShoppingList>> GetListsAsync(CancellationToken cancellationToken = default)
	{
		using var response = await httpClient.GetAsync(BasePath, cancellationToken);
		response.EnsureSuccessStatusCode();
		var lists = await response.Content.ReadFromJsonAsync<IReadOnlyList<ShoppingListResponse>>(cancellationToken);
		return lists?.Select(ToDomain).ToList() ?? [];
	}

	public async Task<ShoppingList?> GetListAsync(int id, CancellationToken cancellationToken = default)
	{
		using var response = await httpClient.GetAsync($"{BasePath}/{id}", cancellationToken);
		return await ReadOptionalListAsync(response, cancellationToken);
	}

	public async Task<ShoppingList> CreateListAsync(string name, string description, CancellationToken cancellationToken = default)
	{
		using var response = await httpClient.PostAsJsonAsync(BasePath, new CreateShoppingListRequest(name, description), cancellationToken);
		response.EnsureSuccessStatusCode();
		return ToDomain((await response.Content.ReadFromJsonAsync<ShoppingListResponse>(cancellationToken))!);
	}

	public Task<ShoppingList?> UpdateListDetailsAsync(int listId, string name, string description, CancellationToken cancellationToken = default)
	{
		return SendForOptionalListAsync(HttpMethod.Put, $"{BasePath}/{listId}", new UpdateShoppingListRequest(name, description), cancellationToken);
	}

	public Task<ShoppingList?> ArchiveListAsync(int listId, CancellationToken cancellationToken = default)
	{
		return SendForOptionalListAsync(HttpMethod.Post, $"{BasePath}/{listId}/archive", content: null, cancellationToken);
	}

	public Task<ShoppingList?> UnarchiveListAsync(int listId, CancellationToken cancellationToken = default)
	{
		return SendForOptionalListAsync(HttpMethod.Post, $"{BasePath}/{listId}/unarchive", content: null, cancellationToken);
	}

	public async Task<bool> RemoveArchivedListAsync(int listId, CancellationToken cancellationToken = default)
	{
		using var response = await httpClient.DeleteAsync($"{BasePath}/{listId}", cancellationToken);
		if (response.StatusCode == HttpStatusCode.NotFound)
		{
			return false;
		}

		response.EnsureSuccessStatusCode();
		return true;
	}

	public Task<ShoppingList?> AddItemAsync(int listId, string itemName, CancellationToken cancellationToken = default)
	{
		return SendForOptionalListAsync(HttpMethod.Post, $"{BasePath}/{listId}/items", new AddShoppingListItemRequest(itemName), cancellationToken);
	}

	public Task<ShoppingList?> RenameItemAsync(int listId, int itemId, string name, CancellationToken cancellationToken = default)
	{
		return SendForOptionalListAsync(HttpMethod.Put, $"{BasePath}/{listId}/items/{itemId}/name", new RenameShoppingListItemRequest(name), cancellationToken);
	}

	public Task<ShoppingList?> ToggleDoneAsync(int listId, int itemId, bool isDone, CancellationToken cancellationToken = default)
	{
		return SendForOptionalListAsync(HttpMethod.Put, $"{BasePath}/{listId}/items/{itemId}/purchased", new ToggleShoppingListItemRequest(isDone), cancellationToken);
	}

	public Task<ShoppingList?> RemoveItemAsync(int listId, int itemId, CancellationToken cancellationToken = default)
	{
		return SendForOptionalListAsync(HttpMethod.Delete, $"{BasePath}/{listId}/items/{itemId}", content: null, cancellationToken);
	}

	public Task<ShoppingList?> ReorderItemAsync(int listId, int itemId, int targetItemId, CancellationToken cancellationToken = default)
	{
		return SendForOptionalListAsync(HttpMethod.Put, $"{BasePath}/{listId}/items/{itemId}/order", new ReorderShoppingListItemRequest(targetItemId), cancellationToken);
	}

	private async Task<ShoppingList?> SendForOptionalListAsync(HttpMethod method, string path, object? content, CancellationToken cancellationToken)
	{
		using var request = new HttpRequestMessage(method, path)
		{
			Content = content is null ? null : JsonContent.Create(content)
		};
		using var response = await httpClient.SendAsync(request, cancellationToken);
		return await ReadOptionalListAsync(response, cancellationToken);
	}

	private static async Task<ShoppingList?> ReadOptionalListAsync(HttpResponseMessage response, CancellationToken cancellationToken)
	{
		if (response.StatusCode == HttpStatusCode.NotFound)
		{
			return null;
		}

		response.EnsureSuccessStatusCode();
		var list = await response.Content.ReadFromJsonAsync<ShoppingListResponse>(cancellationToken);
		return list is null ? null : ToDomain(list);
	}

	private static ShoppingList ToDomain(ShoppingListResponse response)
	{
		return new ShoppingList
		{
			Id = response.Id,
			Name = response.Name,
			Description = response.Description,
			Archived = response.Archived,
			SortOrder = response.SortOrder,
			UpdatedAt = response.UpdatedAt,
			Items = [.. response.Items.Select(ToDomain)]
		};
	}

	private static ShoppingListItem ToDomain(ShoppingListItemResponse response)
	{
		return new ShoppingListItem
		{
			Id = response.Id,
			ShoppingListId = response.ShoppingListId,
			Name = response.Name,
			IsDone = response.IsDone,
			SortOrder = response.SortOrder
		};
	}
}