using Boodschap.Features.Authentication.Application;
using Boodschap.Features.Authentication.Infrastructure.Remote;
using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace Boodschap.Features.Authentication.Tests;

public sealed class RemoteAuthenticationClientTests
{
	[Fact]
	public async Task LoginAsync_StoresTokensAndLoadsCurrentUser()
	{
		var handler = new StubHttpMessageHandler(
			_ => Json(HttpStatusCode.OK, TokenJson("access-1", "refresh-1")),
			request =>
			{
				Assert.Equal(new AuthenticationHeaderValue("Bearer", "access-1"), request.Headers.Authorization);
				return Json(HttpStatusCode.OK, """{"id":7,"username":"andre","isAdmin":true}""");
			});
		var tokenStore = new MemoryTokenStore();
		var client = CreateClient(handler, tokenStore);

		var result = await client.LoginAsync("andre", "secret-value");

		Assert.True(result.Succeeded);
		Assert.Equal(7, result.User!.Id);
		Assert.Equal("andre", result.User.Username);
		Assert.True(result.User.IsAdmin);
		Assert.Equal("access-1", tokenStore.Tokens!.AccessToken);
		Assert.Equal("refresh-1", tokenStore.Tokens.RefreshToken);
		Assert.Equal(2, handler.RequestCount);
	}

	[Fact]
	public async Task GetCurrentUserAsync_OnUnauthorized_RefreshesAndRetriesOnce()
	{
		var handler = new StubHttpMessageHandler(
			_ => Json(HttpStatusCode.Unauthorized, string.Empty),
			_ => Json(HttpStatusCode.OK, TokenJson("access-2", "refresh-2")),
			request =>
			{
				Assert.Equal(new AuthenticationHeaderValue("Bearer", "access-2"), request.Headers.Authorization);
				return Json(HttpStatusCode.OK, """{"id":4,"username":"mobile","isAdmin":false}""");
			});
		var tokenStore = new MemoryTokenStore
		{
			Tokens = new ApiTokenSet("access-1", "refresh-1", DateTimeOffset.UtcNow.AddHours(1))
		};
		var client = CreateClient(handler, tokenStore);

		var user = await client.GetCurrentUserAsync();

		Assert.NotNull(user);
		Assert.Equal("mobile", user.Username);
		Assert.Equal("access-2", tokenStore.Tokens!.AccessToken);
		Assert.Equal(3, handler.RequestCount);
	}

	[Fact]
	public async Task GetCurrentUserAsync_WhenRefreshFails_ClearsTokensAndReturnsAnonymous()
	{
		var handler = new StubHttpMessageHandler(
			_ => Json(HttpStatusCode.Unauthorized, string.Empty),
			_ => Json(HttpStatusCode.Unauthorized, string.Empty));
		var tokenStore = new MemoryTokenStore
		{
			Tokens = new ApiTokenSet("access-1", "refresh-1", DateTimeOffset.UtcNow.AddHours(1))
		};
		var client = CreateClient(handler, tokenStore);

		var user = await client.GetCurrentUserAsync();

		Assert.Null(user);
		Assert.Null(tokenStore.Tokens);
		Assert.Equal(2, handler.RequestCount);
	}

	private static RemoteAuthenticationClient CreateClient(HttpMessageHandler handler, IApiTokenStore tokenStore)
	{
		return new RemoteAuthenticationClient(
			new HttpClient(handler) { BaseAddress = new Uri("https://boodschap.example/") },
			tokenStore);
	}

	private static HttpResponseMessage Json(HttpStatusCode statusCode, string json)
	{
		return new HttpResponseMessage(statusCode)
		{
			Content = new StringContent(json, Encoding.UTF8, "application/json")
		};
	}

	private static string TokenJson(string accessToken, string refreshToken)
	{
		return $$"""{"tokenType":"Bearer","accessToken":"{{accessToken}}","expiresIn":3600,"refreshToken":"{{refreshToken}}"}""";
	}

	private sealed class MemoryTokenStore : IApiTokenStore
	{
		public ApiTokenSet? Tokens { get; set; }

		public Task<ApiTokenSet?> GetAsync() => Task.FromResult(Tokens);

		public Task SetAsync(ApiTokenSet tokens)
		{
			Tokens = tokens;
			return Task.CompletedTask;
		}

		public Task ClearAsync()
		{
			Tokens = null;
			return Task.CompletedTask;
		}
	}

	private sealed class StubHttpMessageHandler(params Func<HttpRequestMessage, HttpResponseMessage>[] responses) : HttpMessageHandler
	{
		private readonly Queue<Func<HttpRequestMessage, HttpResponseMessage>> responses = new(responses);

		public int RequestCount { get; private set; }

		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			RequestCount++;
			return Task.FromResult(responses.Dequeue()(request));
		}
	}
}