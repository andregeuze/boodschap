using Boodschap.Features.ShoppingLists.Infrastructure.Remote;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Boodschap.Features.ShoppingLists.Tests;

public sealed class HttpShoppingListServiceTests
{
	[Fact]
	public async Task GetListsAsync_MapsTransportDtosToDomainModels()
	{
		var handler = new StubHttpMessageHandler(_ => Json(HttpStatusCode.OK, """
			[
			  {
			    "id": 3,
			    "name": "Week",
			    "description": "Boodschappen",
			    "archived": false,
			    "sortOrder": 2,
			    "updatedAt": "2026-08-31T10:00:00Z",
			    "items": [{ "id": 8, "shoppingListId": 3, "name": "Melk", "isDone": true, "sortOrder": 0 }]
			  }
			]
			"""));
		var service = CreateService(handler);

		var list = Assert.Single(await service.GetListsAsync());

		Assert.Equal(3, list.Id);
		Assert.Equal("Week", list.Name);
		var item = Assert.Single(list.Items);
		Assert.Equal("Melk", item.Name);
		Assert.True(item.IsDone);
	}

	[Fact]
	public async Task GetListAsync_WhenServerReturnsNotFound_ReturnsNull()
	{
		var service = CreateService(new StubHttpMessageHandler(_ => Json(HttpStatusCode.NotFound, string.Empty)));

		var result = await service.GetListAsync(404);

		Assert.Null(result);
	}

	[Fact]
	public async Task ReorderItemAsync_SendsTargetItemAndMapsUpdatedList()
	{
		JsonElement? sentBody = null;
		var handler = new StubHttpMessageHandler(async request =>
		{
			Assert.Equal(HttpMethod.Put, request.Method);
			Assert.Equal("https://boodschap.example/api/shopping-lists/5/items/7/order", request.RequestUri?.ToString());
			sentBody = await request.Content!.ReadFromJsonAsync<JsonElement>();
			return Json(HttpStatusCode.OK, ListJson(5));
		});
		var service = CreateService(handler);

		var result = await service.ReorderItemAsync(5, 7, 11);

		Assert.Equal(11, sentBody?.GetProperty("targetItemId").GetInt32());
		Assert.Equal(5, result?.Id);
	}

	private static HttpShoppingListService CreateService(HttpMessageHandler handler)
	{
		return new HttpShoppingListService(new HttpClient(handler)
		{
			BaseAddress = new Uri("https://boodschap.example/")
		});
	}

	private static HttpResponseMessage Json(HttpStatusCode statusCode, string json)
	{
		return new HttpResponseMessage(statusCode)
		{
			Content = new StringContent(json, Encoding.UTF8, "application/json")
		};
	}

	private static string ListJson(int id)
	{
		return $$"""{"id":{{id}},"name":"List","description":"","archived":false,"sortOrder":0,"updatedAt":"2026-08-31T10:00:00Z","items":[]}""";
	}

	private sealed class StubHttpMessageHandler : HttpMessageHandler
	{
		private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> handler;

		public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
			: this(request => Task.FromResult(handler(request)))
		{
		}

		public StubHttpMessageHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler)
		{
			this.handler = handler;
		}

		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			return handler(request);
		}
	}
}