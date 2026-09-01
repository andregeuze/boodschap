using System.Globalization;
using System.Security.Claims;
using Boodschap.Features.Authentication.Application;
using Boodschap.Features.Authentication.Domain;
using Boodschap.Features.Authentication.Infrastructure;
using Microsoft.AspNetCore.Components.Authorization;

namespace Boodschap.Mobile;

public sealed class MobileAuthenticationStateProvider(IRemoteAuthenticationClient authenticationClient)
	: AuthenticationStateProvider, ILocalAuthenticationSession
{
	private const string AuthenticationType = "Boodschap.Mobile";
	private static readonly AuthenticationState AnonymousState = new(new ClaimsPrincipal(new ClaimsIdentity()));
	private Task<AuthenticationState>? authenticationState;

	public override Task<AuthenticationState> GetAuthenticationStateAsync()
	{
		return authenticationState ??= LoadAuthenticationStateAsync();
	}

	public async Task SignInAsync(LocalUser user)
	{
		authenticationState = Task.FromResult(CreateAuthenticationState(user));
		NotifyAuthenticationStateChanged(authenticationState);
		await Task.CompletedTask;
	}

	public async Task SignOutAsync()
	{
		await authenticationClient.ClearSessionAsync();
		SetAnonymous();
	}

	public void SetAnonymous()
	{
		authenticationState = Task.FromResult(AnonymousState);
		NotifyAuthenticationStateChanged(authenticationState);
	}

	private async Task<AuthenticationState> LoadAuthenticationStateAsync()
	{
		var user = await authenticationClient.GetCurrentUserAsync();
		if (user is not null)
		{
			return CreateAuthenticationState(user);
		}

		return AnonymousState;
	}

	private static AuthenticationState CreateAuthenticationState(LocalUser user)
	{
		var claims = new List<Claim>
		{
			new(ClaimTypes.NameIdentifier, user.Id.ToString(CultureInfo.InvariantCulture), ClaimValueTypes.String, LocalAuthenticationDefaults.Issuer),
			new(ClaimTypes.Name, user.Username, ClaimValueTypes.String, LocalAuthenticationDefaults.Issuer)
		};

		if (user.IsAdmin)
		{
			claims.Add(new Claim(ClaimTypes.Role, LocalAuthenticationDefaults.AdminRole, ClaimValueTypes.String, LocalAuthenticationDefaults.Issuer));
		}

		var identity = new ClaimsIdentity(claims, AuthenticationType, ClaimTypes.Name, ClaimTypes.Role);
		return new AuthenticationState(new ClaimsPrincipal(identity));
	}
}