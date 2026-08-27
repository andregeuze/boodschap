using System.Security.Claims;
using Boodschap.Features.Authentication.Application;
using Microsoft.AspNetCore.Components.Authorization;

namespace Boodschap.Features.Authentication.Infrastructure;

public sealed class AuthenticationStateCurrentUserAccessor(AuthenticationStateProvider authenticationStateProvider) : ICurrentUserAccessor
{
	public async Task<CurrentUser?> GetCurrentUserAsync(CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();

		var authenticationState = await authenticationStateProvider.GetAuthenticationStateAsync();
		var principal = authenticationState.User;
		if (principal.Identity?.IsAuthenticated != true)
		{
			return null;
		}

		var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier) ?? principal.FindFirst("sub");
		if (userIdClaim is null || string.IsNullOrWhiteSpace(userIdClaim.Value) || !int.TryParse(userIdClaim.Value, out var localUserId))
		{
			return null;
		}

		var displayName = principal.Identity?.Name
			?? principal.FindFirst(ClaimTypes.GivenName)?.Value
			?? principal.FindFirst(ClaimTypes.Email)?.Value
			?? "Unknown user";
		var email = principal.FindFirst(ClaimTypes.Email)?.Value;
		var issuer = string.IsNullOrWhiteSpace(userIdClaim.Issuer) ? LocalAuthenticationDefaults.Issuer : userIdClaim.Issuer;
        var isAdmin = principal.IsInRole(LocalAuthenticationDefaults.AdminRole);

		return new CurrentUser(localUserId, $"{issuer}:{userIdClaim.Value}", displayName, email, isAdmin);
	}

	public async Task<CurrentUser> GetRequiredCurrentUserAsync(CancellationToken cancellationToken = default)
	{
		return await GetCurrentUserAsync(cancellationToken)
			?? throw new InvalidOperationException("No authenticated user is available for the current circuit.");
	}
}