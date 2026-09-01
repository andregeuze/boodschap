namespace Boodschap.Features.Authentication.Application;

public sealed record ApiTokenSet(string AccessToken, string RefreshToken, DateTimeOffset ExpiresAtUtc);

public interface IApiTokenStore
{
	Task<ApiTokenSet?> GetAsync();
	Task SetAsync(ApiTokenSet tokens);
	Task ClearAsync();
}

public interface IRemoteAuthenticationClient : ILocalAuthenticationService
{
	Task<Domain.LocalUser?> GetCurrentUserAsync(CancellationToken cancellationToken = default);
	Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default);
	Task<bool> RefreshAsync(CancellationToken cancellationToken = default);
	Task ClearSessionAsync();
}