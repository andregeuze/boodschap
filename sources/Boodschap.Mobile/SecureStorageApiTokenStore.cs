using Boodschap.Features.Authentication.Application;
using System.Globalization;

namespace Boodschap.Mobile;

public sealed class SecureStorageApiTokenStore : IApiTokenStore
{
	private const string AccessTokenKey = "api-access-token";
	private const string RefreshTokenKey = "api-refresh-token";
	private const string ExpiresAtKey = "api-access-token-expires-at";

	public async Task<ApiTokenSet?> GetAsync()
	{
		var accessToken = await SecureStorage.Default.GetAsync(AccessTokenKey);
		var refreshToken = await SecureStorage.Default.GetAsync(RefreshTokenKey);
		var expiresAtValue = await SecureStorage.Default.GetAsync(ExpiresAtKey);
		if (string.IsNullOrWhiteSpace(accessToken)
			|| string.IsNullOrWhiteSpace(refreshToken)
			|| !DateTimeOffset.TryParse(expiresAtValue, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var expiresAt))
		{
			return null;
		}

		return new ApiTokenSet(accessToken, refreshToken, expiresAt);
	}

	public async Task SetAsync(ApiTokenSet tokens)
	{
		await SecureStorage.Default.SetAsync(AccessTokenKey, tokens.AccessToken);
		await SecureStorage.Default.SetAsync(RefreshTokenKey, tokens.RefreshToken);
		await SecureStorage.Default.SetAsync(ExpiresAtKey, tokens.ExpiresAtUtc.ToString("O", CultureInfo.InvariantCulture));
	}

	public Task ClearAsync()
	{
		SecureStorage.Default.Remove(AccessTokenKey);
		SecureStorage.Default.Remove(RefreshTokenKey);
		SecureStorage.Default.Remove(ExpiresAtKey);
		return Task.CompletedTask;
	}
}