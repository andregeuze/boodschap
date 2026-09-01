using Boodschap.Features.Authentication.Application;
using Boodschap.Features.Authentication.Domain;
using Boodschap.Features.Authentication.Infrastructure;
using Boodschap.Shared.Infrastructure.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Security.Claims;

namespace Boodschap.Features.Authentication.Presentation;

public static class AuthenticationApiEndpoints
{
	public static void MapAuthenticationApiEndpoints(this IEndpointRouteBuilder endpoints)
	{
		var group = endpoints.MapGroup("/api/auth")
			.WithTags("Authentication");

		group.MapPost("/login", LoginAsync)
			.AllowAnonymous()
			.RequireRateLimiting(LocalAuthenticationDefaults.MobileAuthenticationRateLimitPolicy);

		group.MapPost("/refresh", RefreshAsync)
			.AllowAnonymous()
			.RequireRateLimiting(LocalAuthenticationDefaults.MobileAuthenticationRateLimitPolicy);

		group.MapGet("/me", GetCurrentUser)
			.RequireAuthorization(CreateBearerPolicy());
	}

	internal static async Task<IResult> LoginAsync(
		AuthenticationLoginRequest request,
		ILocalAuthenticationService authenticationService,
		CancellationToken cancellationToken)
	{
		if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
		{
			return Results.BadRequest(new AuthenticationErrorResponse(LocalAuthenticationErrorCodes.InvalidCredentials));
		}

		var result = await authenticationService.LoginAsync(request.Username, request.Password, cancellationToken);
		if (!result.Succeeded || result.User is null)
		{
			return Results.Json(
				new AuthenticationErrorResponse(LocalAuthenticationErrorCodes.InvalidCredentials),
				statusCode: StatusCodes.Status401Unauthorized);
		}

		return Results.SignIn(CreatePrincipal(result.User), authenticationScheme: ApiAuthenticationDefaults.BearerScheme);
	}

	internal static IResult RefreshAsync(
		AuthenticationRefreshRequest request,
		IOptionsMonitor<BearerTokenOptions> options)
	{
		if (string.IsNullOrWhiteSpace(request.RefreshToken))
		{
			return Results.Unauthorized();
		}

		var ticket = options
			.Get(ApiAuthenticationDefaults.BearerScheme)
			.RefreshTokenProtector
			.Unprotect(request.RefreshToken);

		if (ticket?.Properties.ExpiresUtc is not { } expiresUtc || expiresUtc <= DateTimeOffset.UtcNow)
		{
			return Results.Unauthorized();
		}

		return Results.SignIn(ticket.Principal, authenticationScheme: ApiAuthenticationDefaults.BearerScheme);
	}

	internal static IResult GetCurrentUser(ClaimsPrincipal principal)
	{
		if (!int.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), CultureInfo.InvariantCulture, out var userId)
			|| string.IsNullOrWhiteSpace(principal.Identity?.Name))
		{
			return Results.Unauthorized();
		}

		return Results.Ok(new AuthenticationUserResponse(
			userId,
			principal.Identity.Name,
			principal.IsInRole(LocalAuthenticationDefaults.AdminRole)));
	}

	private static AuthorizationPolicy CreateBearerPolicy()
	{
		return new AuthorizationPolicyBuilder(ApiAuthenticationDefaults.BearerScheme)
			.RequireAuthenticatedUser()
			.Build();
	}

	private static ClaimsPrincipal CreatePrincipal(LocalUser user)
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

		return new ClaimsPrincipal(new ClaimsIdentity(claims, ApiAuthenticationDefaults.BearerScheme, ClaimTypes.Name, ClaimTypes.Role));
	}
}