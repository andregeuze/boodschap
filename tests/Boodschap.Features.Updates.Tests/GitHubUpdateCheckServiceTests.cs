using System.Net;
using Boodschap.Features.Updates;
using Boodschap.Features.Updates.Domain;
using Boodschap.Features.Updates.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Boodschap.Features.Updates.Tests;

public sealed class GitHubUpdateCheckServiceTests
{
	[Fact]
	public async Task CheckAsync_WhenCommitsMatch_ReturnsUpToDate()
	{
		var handler = new StubHttpMessageHandler(HttpStatusCode.OK, CreateCommitJson("abcdef123456"));
		var service = CreateService(handler, "abcdef1");

		var result = await service.CheckAsync();

		Assert.Equal(UpdateAvailability.UpToDate, result.Availability);
		Assert.Equal("abcdef123456", result.LatestCommit);
	}

	[Fact]
	public async Task CheckAsync_WhenLatestCommitDiffers_ReturnsUpdateLink()
	{
		var handler = new StubHttpMessageHandler(HttpStatusCode.OK, CreateCommitJson("fedcba654321"));
		var service = CreateService(handler, "abcdef123456");

		var result = await service.CheckAsync();

		Assert.Equal(UpdateAvailability.UpdateAvailable, result.Availability);
		Assert.Equal(new Uri("https://github.com/andregeuze/boodschap/commit/fedcba654321"), result.LatestCommitUrl);
	}

	[Fact]
	public async Task CheckAsync_WhenCurrentCommitIsMissing_DoesNotCallGitHub()
	{
		var handler = new StubHttpMessageHandler(HttpStatusCode.OK, CreateCommitJson("abcdef123456"));
		var service = CreateService(handler, "unknown");

		var result = await service.CheckAsync();

		Assert.Equal(UpdateAvailability.Unavailable, result.Availability);
		Assert.Equal(0, handler.RequestCount);
	}

	[Fact]
	public async Task CheckAsync_WhenGitHubFails_ReturnsUnavailable()
	{
		var handler = new StubHttpMessageHandler(HttpStatusCode.ServiceUnavailable, "{}");
		var service = CreateService(handler, "abcdef123456");

		var result = await service.CheckAsync();

		Assert.Equal(UpdateAvailability.Unavailable, result.Availability);
		Assert.Equal(2, handler.RequestCount);
	}

	[Fact]
	public async Task CheckAsync_WhenGatewayTimesOutOnce_RetriesAndReturnsLatestCommit()
	{
		var handler = new StubHttpMessageHandler(
			(HttpStatusCode.GatewayTimeout, "{}"),
			(HttpStatusCode.OK, CreateCommitJson("abcdef123456")));
		var service = CreateService(handler, "abcdef123456");

		var result = await service.CheckAsync();

		Assert.Equal(UpdateAvailability.UpToDate, result.Availability);
		Assert.Equal(2, handler.RequestCount);
	}

	[Fact]
	public async Task CheckAsync_ReusesCachedResult()
	{
		var handler = new StubHttpMessageHandler(HttpStatusCode.OK, CreateCommitJson("abcdef123456"));
		var service = CreateService(handler, "abcdef123456");

		await service.CheckAsync();
		await service.CheckAsync();

		Assert.Equal(1, handler.RequestCount);
	}

	private static GitHubUpdateCheckService CreateService(StubHttpMessageHandler handler, string currentCommit)
	{
		var options = Options.Create(new UpdateFeatureOptions
		{
			CurrentCommit = currentCommit,
			CacheDurationMinutes = 15
		});

		return new GitHubUpdateCheckService(
			new StubHttpClientFactory(handler),
			options,
			NullLogger<GitHubUpdateCheckService>.Instance);
	}

	private static string CreateCommitJson(string commit)
	{
		return $$"""
			{
			  "sha": "{{commit}}",
			  "html_url": "https://github.com/andregeuze/boodschap/commit/{{commit}}"
			}
			""";
	}

	private sealed class StubHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
	{
		public HttpClient CreateClient(string name)
		{
			return new HttpClient(handler, disposeHandler: false)
			{
				BaseAddress = new Uri("https://api.github.com/")
			};
		}
	}

	private sealed class StubHttpMessageHandler : HttpMessageHandler
	{
		private readonly Queue<(HttpStatusCode StatusCode, string Content)> responses;
		private (HttpStatusCode StatusCode, string Content) lastResponse;

		public StubHttpMessageHandler(HttpStatusCode statusCode, string content)
			: this((statusCode, content))
		{
		}

		public StubHttpMessageHandler(params (HttpStatusCode StatusCode, string Content)[] responses)
		{
			this.responses = new Queue<(HttpStatusCode StatusCode, string Content)>(responses);
			lastResponse = responses[^1];
		}

		public int RequestCount { get; private set; }

		protected override Task<HttpResponseMessage> SendAsync(
			HttpRequestMessage request,
			CancellationToken cancellationToken)
		{
			RequestCount++;
			var response = responses.TryDequeue(out var nextResponse) ? nextResponse : lastResponse;
			lastResponse = response;
			return Task.FromResult(new HttpResponseMessage(response.StatusCode)
			{
				Content = new StringContent(response.Content)
			});
		}
	}
}