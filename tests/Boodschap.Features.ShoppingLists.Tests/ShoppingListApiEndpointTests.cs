using Boodschap.Features.ShoppingLists.Application;
using Boodschap.Features.ShoppingLists.Domain;
using Boodschap.Features.ShoppingLists.Presentation;
using Boodschap.Features.ShoppingLists.Tests.Testing;
using Boodschap.Shared.Infrastructure.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace Boodschap.Features.ShoppingLists.Tests;

public sealed class ShoppingListApiEndpointTests
{
	[Fact]
	public async Task CreateListAsync_ReturnsCreatedTransportDto()
	{
		var service = new FakeShoppingListService();

		var result = await ShoppingListApiEndpoints.CreateListAsync(
			new CreateShoppingListRequest("Weekend", "Ontbijt"),
			service,
			CancellationToken.None);

		Assert.Equal(StatusCodes.Status201Created, Assert.IsAssignableFrom<IStatusCodeHttpResult>(result).StatusCode);
		var response = Assert.IsType<ShoppingListResponse>(Assert.IsAssignableFrom<IValueHttpResult>(result).Value);
		Assert.Equal("Weekend", response.Name);
		Assert.Equal("Ontbijt", response.Description);
		Assert.Equal("Weekend", service.LastCreatedListName);
	}

	[Fact]
	public async Task CreateListAsync_WithBlankName_ReturnsValidationProblem()
	{
		var service = new FakeShoppingListService();

		var result = await ShoppingListApiEndpoints.CreateListAsync(
			new CreateShoppingListRequest(" ", string.Empty),
			service,
			CancellationToken.None);

		Assert.Equal(StatusCodes.Status400BadRequest, Assert.IsAssignableFrom<IStatusCodeHttpResult>(result).StatusCode);
		Assert.Null(service.LastCreatedListName);
	}

	[Fact]
	public async Task AddItemAsync_ReturnsUpdatedListWithoutGuessingCreatedItemId()
	{
		var service = new FakeShoppingListService(
		[
			new ShoppingList { Id = 9, Name = "Active", Items = [] }
		]);

		var result = await ShoppingListApiEndpoints.AddItemAsync(
			9,
			new AddShoppingListItemRequest("Melk"),
			service,
			CancellationToken.None);

		Assert.Equal(StatusCodes.Status200OK, Assert.IsAssignableFrom<IStatusCodeHttpResult>(result).StatusCode);
		var response = Assert.IsType<ShoppingListResponse>(Assert.IsAssignableFrom<IValueHttpResult>(result).Value);
		Assert.Contains(response.Items, item => item.Name == "Melk");
	}

	[Fact]
	public async Task DeleteListAsync_WhenListCannotBeRemoved_ReturnsNotFound()
	{
		var service = new FakeShoppingListService(
		[
			new ShoppingList { Id = 9, Name = "Active", Archived = false, Items = [] }
		]);

		var result = await ShoppingListApiEndpoints.DeleteListAsync(9, service, CancellationToken.None);

		Assert.Equal(StatusCodes.Status404NotFound, Assert.IsAssignableFrom<IStatusCodeHttpResult>(result).StatusCode);
	}

	[Fact]
	public void ShoppingListUpdatesHub_RequiresMobileBearerScheme()
	{
		var authorize = Assert.Single(typeof(ShoppingListUpdatesHub)
			.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
			.Cast<AuthorizeAttribute>());

		Assert.Equal(ApiAuthenticationDefaults.BearerScheme, authorize.AuthenticationSchemes);
	}
}