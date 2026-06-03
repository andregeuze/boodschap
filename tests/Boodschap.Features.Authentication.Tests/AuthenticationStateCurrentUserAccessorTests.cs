using System.Security.Claims;
using Boodschap.Features.Authentication.Infrastructure;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;

namespace Boodschap.Features.Authentication.Tests;

public sealed class AuthenticationStateCurrentUserAccessorTests
{
	[Fact]
	public async Task GetCurrentUserAsync_ReturnsNullWhenPrincipalIsAnonymous()
	{
		var accessor = new AuthenticationStateCurrentUserAccessor(new StubAuthenticationStateProvider(new ClaimsPrincipal(new ClaimsIdentity())));

		var result = await accessor.GetCurrentUserAsync();

		Assert.Null(result);
	}

	[Fact]
	public async Task GetCurrentUserAsync_MapsStableIdentityFromClaimsPrincipal()
	{
		var identity = new ClaimsIdentity(
		[
			new(ClaimTypes.NameIdentifier, "12345", ClaimValueTypes.String, "boodschap-local"),
			new(ClaimTypes.Name, "Andre"),
			new(ClaimTypes.Role, LocalAuthenticationDefaults.AdminRole, ClaimValueTypes.String, LocalAuthenticationDefaults.Issuer)
		],
		CookieAuthenticationDefaults.AuthenticationScheme);
		var accessor = new AuthenticationStateCurrentUserAccessor(new StubAuthenticationStateProvider(new ClaimsPrincipal(identity)));

		var result = await accessor.GetCurrentUserAsync();

		Assert.NotNull(result);
		Assert.Equal(12345, result.LocalUserId);
		Assert.Equal("boodschap-local:12345", result.Id);
		Assert.Equal("Andre", result.DisplayName);
		Assert.Null(result.Email);
		Assert.True(result.IsAdmin);
	}

	[Fact]
	public async Task GetRequiredCurrentUserAsync_ThrowsWhenPrincipalIsAnonymous()
	{
		var accessor = new AuthenticationStateCurrentUserAccessor(new StubAuthenticationStateProvider(new ClaimsPrincipal(new ClaimsIdentity())));

		await Assert.ThrowsAsync<InvalidOperationException>(() => accessor.GetRequiredCurrentUserAsync());
	}

	private sealed class StubAuthenticationStateProvider(ClaimsPrincipal principal) : AuthenticationStateProvider
	{
		public override Task<AuthenticationState> GetAuthenticationStateAsync()
		{
			return Task.FromResult(new AuthenticationState(principal));
		}
	}
}