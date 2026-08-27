using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Boodschap.Features.Updates.Application.Contracts;
using Boodschap.Features.Updates.Domain;
using Microsoft.Extensions.Options;

namespace Boodschap.Features.Updates.Infrastructure;

public sealed class GitHubUpdateCheckService(
	IHttpClientFactory httpClientFactory,
	IOptions<UpdateFeatureOptions> options,
	ILogger<GitHubUpdateCheckService> logger) : IUpdateCheckService
{
	private const int MaxAttempts = 2;
	private static readonly TimeSpan RetryDelay = TimeSpan.FromMilliseconds(250);
	private readonly SemaphoreSlim checkLock = new(1, 1);
	private UpdateCheckResult? cachedResult;
	private DateTimeOffset cachedAt;

	public async Task<UpdateCheckResult> CheckAsync(CancellationToken cancellationToken = default)
	{
		var cacheDuration = TimeSpan.FromMinutes(Math.Max(1, options.Value.CacheDurationMinutes));
		if (cachedResult is not null && DateTimeOffset.UtcNow - cachedAt < cacheDuration)
		{
			return cachedResult;
		}

		await checkLock.WaitAsync(cancellationToken);
		try
		{
			if (cachedResult is not null && DateTimeOffset.UtcNow - cachedAt < cacheDuration)
			{
				return cachedResult;
			}

			var result = await CheckGitHubAsync(cancellationToken);
			if (result.Availability != UpdateAvailability.Unavailable)
			{
				cachedResult = result;
				cachedAt = DateTimeOffset.UtcNow;
			}

			return result;
		}
		finally
		{
			checkLock.Release();
		}
	}

	private async Task<UpdateCheckResult> CheckGitHubAsync(CancellationToken cancellationToken)
	{
		var currentCommit = ResolveCurrentCommit();
		if (!IsCommitHash(currentCommit))
		{
			logger.LogWarning("The running commit is unknown; update availability cannot be determined.");
			return new UpdateCheckResult(UpdateAvailability.Unavailable);
		}

		try
		{
			var owner = Uri.EscapeDataString(options.Value.Owner);
			var repository = Uri.EscapeDataString(options.Value.Repository);
			var branch = Uri.EscapeDataString(options.Value.Branch);
			var client = httpClientFactory.CreateClient(UpdatesModule.HttpClientName);
			var latest = await GetLatestCommitAsync(
				client,
				$"repos/{owner}/{repository}/commits/{branch}",
				cancellationToken);

			if (latest is null || !IsCommitHash(latest.Sha))
			{
				return new UpdateCheckResult(UpdateAvailability.Unavailable, currentCommit);
			}

			var availability = CommitsMatch(currentCommit, latest.Sha)
				? UpdateAvailability.UpToDate
				: UpdateAvailability.UpdateAvailable;

			return new UpdateCheckResult(
				availability,
				currentCommit,
				latest.Sha,
				Uri.TryCreate(latest.HtmlUrl, UriKind.Absolute, out var latestCommitUrl) ? latestCommitUrl : null);
		}
		catch (Exception exception) when (exception is HttpRequestException or JsonException)
		{
			logger.LogInformation("Checking GitHub for a newer Boodschap commit failed: {Message}", exception.Message);
			return new UpdateCheckResult(UpdateAvailability.Unavailable, currentCommit);
		}
		catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
		{
			logger.LogInformation("Checking GitHub for a newer Boodschap commit timed out: {Message}", exception.Message);
			return new UpdateCheckResult(UpdateAvailability.Unavailable, currentCommit);
		}
	}

	private async Task<GitHubCommit?> GetLatestCommitAsync(
		HttpClient client,
		string requestUri,
		CancellationToken cancellationToken)
	{
		for (var attempt = 1; attempt <= MaxAttempts; attempt++)
		{
			try
			{
				using var response = await client.GetAsync(
					requestUri,
					HttpCompletionOption.ResponseHeadersRead,
					cancellationToken);

				if (response.IsSuccessStatusCode)
				{
					return await response.Content.ReadFromJsonAsync<GitHubCommit>(cancellationToken);
				}

				if (!IsTransient(response.StatusCode) || attempt == MaxAttempts)
				{
					logger.LogInformation(
						"Checking GitHub for a newer Boodschap commit returned HTTP {StatusCode}.",
						(int)response.StatusCode);
					return null;
				}
			}
			catch (HttpRequestException) when (attempt < MaxAttempts)
			{
			}
			catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested && attempt < MaxAttempts)
			{
			}

			await Task.Delay(RetryDelay, cancellationToken);
		}

		return null;
	}

	private string? ResolveCurrentCommit()
	{
		if (!string.IsNullOrWhiteSpace(options.Value.CurrentCommit))
		{
			return options.Value.CurrentCommit.Trim();
		}

		return typeof(GitHubUpdateCheckService).Assembly
			.GetCustomAttributes<AssemblyMetadataAttribute>()
			.FirstOrDefault(attribute => attribute.Key == "RepositoryCommit")?
			.Value;
	}

	private static bool CommitsMatch(string currentCommit, string latestCommit)
	{
		return currentCommit.StartsWith(latestCommit, StringComparison.OrdinalIgnoreCase)
			|| latestCommit.StartsWith(currentCommit, StringComparison.OrdinalIgnoreCase);
	}

	private static bool IsCommitHash([NotNullWhen(true)] string? commit)
	{
		return commit is { Length: >= 7 and <= 64 }
			&& commit.All(character => char.IsAsciiHexDigit(character));
	}

	private static bool IsTransient(HttpStatusCode statusCode)
	{
		return statusCode is HttpStatusCode.RequestTimeout
			or HttpStatusCode.TooManyRequests
			or HttpStatusCode.InternalServerError
			or HttpStatusCode.BadGateway
			or HttpStatusCode.ServiceUnavailable
			or HttpStatusCode.GatewayTimeout;
	}

	private sealed record GitHubCommit(
		[property: JsonPropertyName("sha")] string Sha,
		[property: JsonPropertyName("html_url")] string? HtmlUrl);
}