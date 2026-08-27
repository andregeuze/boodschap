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

	private sealed class StubHttpMessageHandler(HttpStatusCode statusCode, string content) : HttpMessageHandler
	{
		public int RequestCount { get; private set; }

		protected override Task<HttpResponseMessage> SendAsync(
			HttpRequestMessage request,
			CancellationToken cancellationToken)
		{
			RequestCount++;
			return Task.FromResult(new HttpResponseMessage(statusCode)
			{
				Content = new StringContent(content)
			});
		}
	}
}