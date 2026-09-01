using Boodschap.Features.ShoppingLists.Application;
using Boodschap.Shared.Infrastructure.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Boodschap.Features.ShoppingLists.Presentation;

[Authorize(AuthenticationSchemes = ApiAuthenticationDefaults.BearerScheme)]
internal sealed class ShoppingListUpdatesHub : Hub;

public static class ShoppingListUpdatesHubEndpoints
{
	public static void MapShoppingListUpdatesHub(this IEndpointRouteBuilder endpoints)
	{
		endpoints.MapHub<ShoppingListUpdatesHub>(StoreChangeRealtimeDefaults.HubRoute);
	}
}