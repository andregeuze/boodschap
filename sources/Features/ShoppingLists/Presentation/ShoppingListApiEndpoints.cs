using Boodschap.Features.ShoppingLists.Application;
using Boodschap.Shared.Infrastructure.Authentication;
using Microsoft.AspNetCore.Authorization;

namespace Boodschap.Features.ShoppingLists.Presentation;

public static class ShoppingListApiEndpoints
{
	public static void MapShoppingListApiEndpoints(this IEndpointRouteBuilder endpoints)
	{
		var group = endpoints.MapGroup("/api/shopping-lists")
			.WithTags("Shopping Lists")
			.RequireAuthorization(CreateBearerPolicy());

		group.MapGet("/", GetListsAsync);
		group.MapGet("/{listId:int}", GetListAsync);
		group.MapPost("/", CreateListAsync);
		group.MapPut("/{listId:int}", UpdateListAsync);
		group.MapPost("/{listId:int}/archive", ArchiveListAsync);
		group.MapPost("/{listId:int}/unarchive", UnarchiveListAsync);
		group.MapDelete("/{listId:int}", DeleteListAsync);
		group.MapPost("/{listId:int}/items", AddItemAsync);
		group.MapPut("/{listId:int}/items/{itemId:int}/name", RenameItemAsync);
		group.MapPut("/{listId:int}/items/{itemId:int}/purchased", ToggleItemAsync);
		group.MapDelete("/{listId:int}/items/{itemId:int}", RemoveItemAsync);
		group.MapPut("/{listId:int}/items/{itemId:int}/order", ReorderItemAsync);
	}

	internal static async Task<IResult> GetListsAsync(IShoppingListService service, CancellationToken cancellationToken)
	{
		var lists = await service.GetListsAsync(cancellationToken);
		return Results.Ok(lists.Select(ShoppingListApiMapper.ToResponse));
	}

	internal static async Task<IResult> GetListAsync(int listId, IShoppingListService service, CancellationToken cancellationToken)
	{
		var list = await service.GetListAsync(listId, cancellationToken);
		return list is null ? Results.NotFound() : Results.Ok(list.ToResponse());
	}

	internal static async Task<IResult> CreateListAsync(CreateShoppingListRequest request, IShoppingListService service, CancellationToken cancellationToken)
	{
		if (InvalidName(request.Name) is { } validationProblem)
		{
			return validationProblem;
		}

		var list = await service.CreateListAsync(request.Name, request.Description ?? string.Empty, cancellationToken);
		return Results.Created($"/api/shopping-lists/{list.Id}", list.ToResponse());
	}

	internal static async Task<IResult> UpdateListAsync(int listId, UpdateShoppingListRequest request, IShoppingListService service, CancellationToken cancellationToken)
	{
		if (InvalidName(request.Name) is { } validationProblem)
		{
			return validationProblem;
		}

		return ToListResult(await service.UpdateListDetailsAsync(listId, request.Name, request.Description ?? string.Empty, cancellationToken));
	}

	internal static async Task<IResult> ArchiveListAsync(int listId, IShoppingListService service, CancellationToken cancellationToken)
	{
		return ToListResult(await service.ArchiveListAsync(listId, cancellationToken));
	}

	internal static async Task<IResult> UnarchiveListAsync(int listId, IShoppingListService service, CancellationToken cancellationToken)
	{
		return ToListResult(await service.UnarchiveListAsync(listId, cancellationToken));
	}

	internal static async Task<IResult> DeleteListAsync(int listId, IShoppingListService service, CancellationToken cancellationToken)
	{
		return await service.RemoveArchivedListAsync(listId, cancellationToken)
			? Results.NoContent()
			: Results.NotFound();
	}

	internal static async Task<IResult> AddItemAsync(int listId, AddShoppingListItemRequest request, IShoppingListService service, CancellationToken cancellationToken)
	{
		if (InvalidName(request.Name) is { } validationProblem)
		{
			return validationProblem;
		}

		var list = await service.AddItemAsync(listId, request.Name, cancellationToken);
		if (list is null)
		{
			return Results.NotFound();
		}

		return Results.Ok(list.ToResponse());
	}

	internal static async Task<IResult> RenameItemAsync(int listId, int itemId, RenameShoppingListItemRequest request, IShoppingListService service, CancellationToken cancellationToken)
	{
		if (InvalidName(request.Name) is { } validationProblem)
		{
			return validationProblem;
		}

		return ToListResult(await service.RenameItemAsync(listId, itemId, request.Name, cancellationToken));
	}

	internal static async Task<IResult> ToggleItemAsync(int listId, int itemId, ToggleShoppingListItemRequest request, IShoppingListService service, CancellationToken cancellationToken)
	{
		return ToListResult(await service.ToggleDoneAsync(listId, itemId, request.IsDone, cancellationToken));
	}

	internal static async Task<IResult> RemoveItemAsync(int listId, int itemId, IShoppingListService service, CancellationToken cancellationToken)
	{
		return ToListResult(await service.RemoveItemAsync(listId, itemId, cancellationToken));
	}

	internal static async Task<IResult> ReorderItemAsync(int listId, int itemId, ReorderShoppingListItemRequest request, IShoppingListService service, CancellationToken cancellationToken)
	{
		return ToListResult(await service.ReorderItemAsync(listId, itemId, request.TargetItemId, cancellationToken));
	}

	private static IResult ToListResult(Domain.ShoppingList? list)
	{
		return list is null ? Results.NotFound() : Results.Ok(list.ToResponse());
	}

	private static IResult? InvalidName(string? name)
	{
		return string.IsNullOrWhiteSpace(name)
			? Results.ValidationProblem(new Dictionary<string, string[]> { ["name"] = ["A name is required."] })
			: null;
	}

	private static AuthorizationPolicy CreateBearerPolicy()
	{
		return new AuthorizationPolicyBuilder(ApiAuthenticationDefaults.BearerScheme)
			.RequireAuthenticatedUser()
			.Build();
	}
}