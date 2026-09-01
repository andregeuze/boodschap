using Boodschap.Features.Authentication.Application;
using Boodschap.Features.Authentication.Domain;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Boodschap.Features.Authentication.Infrastructure.Remote;

public sealed class RemoteAuthenticationClient(HttpClient httpClient, IApiTokenStore tokenStore)
	: IRemoteAuthenticationClient
{
	private static readonly TimeSpan RefreshSkew = TimeSpan.FromMinutes(1);
	private readonly SemaphoreSlim refreshLock = new(1, 1);

	public async Task<LocalAuthenticationResult> LoginAsync(string username, string password, CancellationToken cancellationToken = default)
	{
		using var response = await httpClient.PostAsJsonAsync(
			"api/auth/login",
			new AuthenticationLoginRequest(username, password),
			cancellationToken);

		if (response.StatusCode == HttpStatusCode.Unauthorized)
		{
			return LocalAuthenticationResult.Failure(LocalAuthenticationErrorCodes.InvalidCredentials);
		}

		response.EnsureSuccessStatusCode();
		var tokens = await response.Content.ReadFromJsonAsync<AuthenticationTokenResponse>(cancellationToken);
		if (tokens is null)
		{
			throw new HttpRequestException("The authentication response did not contain tokens.");
		}

		await SaveTokensAsync(tokens);
		var user = await GetCurrentUserAsync(cancellationToken);
		if (user is null)
		{
			return LocalAuthenticationResult.Failure(LocalAuthenticationErrorCodes.InvalidCredentials);
		}

		return LocalAuthenticationResult.Success(user);
	}

	public Task<bool> IsBootstrapRegistrationOpenAsync(CancellationToken cancellationToken = default)
	{
		return Task.FromResult(false);
	}

	public Task<LocalAuthenticationResult> RegisterAsync(string username, string password, string confirmPassword, CancellationToken cancellationToken = default)
	{
		return Task.FromResult(LocalAuthenticationResult.Failure(LocalAuthenticationErrorCodes.BootstrapRegistrationClosed));
	}

	public Task<LocalAuthenticationResult> CreateUserAsync(int actorUserId, string username, string password, string confirmPassword, bool isAdmin, CancellationToken cancellationToken = default)
	{
		return Task.FromResult(LocalAuthenticationResult.Failure(LocalAuthenticationErrorCodes.AdminRequired));
	}

	public Task<LocalPasswordChangeResult> ChangePasswordAsync(int actorUserId, string currentPassword, string newPassword, string confirmPassword, CancellationToken cancellationToken = default)
	{
		return Task.FromResult(LocalPasswordChangeResult.Failure(LocalAuthenticationErrorCodes.AdminRequired));
	}

	public async Task<LocalUser?> GetCurrentUserAsync(CancellationToken cancellationToken = default)
	{
		var accessToken = await GetAccessTokenAsync(cancellationToken);
		if (string.IsNullOrWhiteSpace(accessToken))
		{
			return null;
		}

		using var firstResponse = await SendMeAsync(accessToken, cancellationToken);
		if (firstResponse.StatusCode != HttpStatusCode.Unauthorized)
		{
			return await ReadUserAsync(firstResponse, cancellationToken);
		}

		if (!await RefreshAsync(cancellationToken))
		{
			return null;
		}

		accessToken = (await tokenStore.GetAsync())?.AccessToken;
		using var retryResponse = await SendMeAsync(accessToken!, cancellationToken);
		return await ReadUserAsync(retryResponse, cancellationToken);
	}

	public async Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default)
	{
		var tokens = await tokenStore.GetAsync();
		if (tokens is null)
		{
			return null;
		}

		if (tokens.ExpiresAtUtc > DateTimeOffset.UtcNow.Add(RefreshSkew))
		{
			return tokens.AccessToken;
		}

		return await RefreshAsync(cancellationToken)
			? (await tokenStore.GetAsync())?.AccessToken
			: null;
	}

	public async Task<bool> RefreshAsync(CancellationToken cancellationToken = default)
	{
		await refreshLock.WaitAsync(cancellationToken);
		try
		{
			var currentTokens = await tokenStore.GetAsync();
			if (currentTokens is null || string.IsNullOrWhiteSpace(currentTokens.RefreshToken))
			{
				await tokenStore.ClearAsync();
				return false;
			}

			using var response = await httpClient.PostAsJsonAsync(
				"api/auth/refresh",
				new AuthenticationRefreshRequest(currentTokens.RefreshToken),
				cancellationToken);
			if (!response.IsSuccessStatusCode)
			{
				await tokenStore.ClearAsync();
				return false;
			}

			var refreshedTokens = await response.Content.ReadFromJsonAsync<AuthenticationTokenResponse>(cancellationToken);
			if (refreshedTokens is null)
			{
				await tokenStore.ClearAsync();
				return false;
			}

			await SaveTokensAsync(refreshedTokens);
			return true;
		}
		finally
		{
			refreshLock.Release();
		}
	}

	public Task ClearSessionAsync()
	{
		return tokenStore.ClearAsync();
	}

	private async Task SaveTokensAsync(AuthenticationTokenResponse response)
	{
		await tokenStore.SetAsync(new ApiTokenSet(
			response.AccessToken,
			response.RefreshToken,
			DateTimeOffset.UtcNow.AddSeconds(response.ExpiresIn)));
	}

	private Task<HttpResponseMessage> SendMeAsync(string accessToken, CancellationToken cancellationToken)
	{
		var request = new HttpRequestMessage(HttpMethod.Get, "api/auth/me");
		request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
		return httpClient.SendAsync(request, cancellationToken);
	}

	private async Task<LocalUser?> ReadUserAsync(HttpResponseMessage response, CancellationToken cancellationToken)
	{
		if (response.StatusCode == HttpStatusCode.Unauthorized)
		{
			await tokenStore.ClearAsync();
			return null;
		}

		response.EnsureSuccessStatusCode();
		var user = await response.Content.ReadFromJsonAsync<AuthenticationUserResponse>(cancellationToken);
		return user is null
			? null
			: new LocalUser { Id = user.Id, Username = user.Username, IsAdmin = user.IsAdmin };
	}
}