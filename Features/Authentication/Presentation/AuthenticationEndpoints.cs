using Boodschap.Features.Authentication.Application;
using Boodschap.Features.Authentication.Infrastructure;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using System.Globalization;
using System.Security.Claims;

namespace Boodschap.Features.Authentication.Presentation;

public static class AuthenticationEndpoints
{
	public static void MapAuthenticationEndpoints(this IEndpointRouteBuilder endpoints)
	{
		endpoints.MapPost("/account/login", async (HttpContext httpContext, IAntiforgery antiforgery, ILocalAuthenticationService authenticationService, CancellationToken cancellationToken) =>
			{
				await antiforgery.ValidateRequestAsync(httpContext);
				var form = await httpContext.Request.ReadFormAsync(cancellationToken);
				var returnUrl = ReturnUrlSanitizer.Normalize(form["returnUrl"].ToString());
				var username = form["username"].ToString();
				var password = form["password"].ToString();
				var result = await authenticationService.LoginAsync(username, password, cancellationToken);
				if (!result.Succeeded || result.User is null)
				{
					return Results.Redirect(BuildSignInUrl(returnUrl, result.ErrorCode, username));
				}

				await httpContext.SignInAsync(
					CookieAuthenticationDefaults.AuthenticationScheme,
					CreatePrincipal(result.User));

				return Results.Redirect(returnUrl);
			})
			.AllowAnonymous();

		endpoints.MapPost("/account/register", async (HttpContext httpContext, IAntiforgery antiforgery, ILocalAuthenticationService authenticationService, CancellationToken cancellationToken) =>
			{
				await antiforgery.ValidateRequestAsync(httpContext);
				var form = await httpContext.Request.ReadFormAsync(cancellationToken);
				var returnUrl = ReturnUrlSanitizer.Normalize(form["returnUrl"].ToString());
				var username = form["username"].ToString();
				var password = form["password"].ToString();
				var confirmPassword = form["confirmPassword"].ToString();
				var result = await authenticationService.RegisterAsync(
					username,
					password,
					confirmPassword,
					cancellationToken);

				if (!result.Succeeded || result.User is null)
				{
					return Results.Redirect(BuildSignInUrl(returnUrl, result.ErrorCode, username));
				}

				await httpContext.SignInAsync(
					CookieAuthenticationDefaults.AuthenticationScheme,
					CreatePrincipal(result.User));

				return Results.Redirect(returnUrl);
			})
			.AllowAnonymous();

		endpoints.MapGet("/account/logout", async (HttpContext httpContext, string? returnUrl) =>
			{
				await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
				return Results.Redirect(ReturnUrlSanitizer.Normalize(returnUrl ?? "/signed-out"));
			})
			.AllowAnonymous();
	}

	private static string BuildSignInUrl(string returnUrl, string? errorCode, string? username)
	{
		var usernameSegment = string.IsNullOrWhiteSpace(username)
			? string.Empty
			: $"&username={Uri.EscapeDataString(username)}";

		if (string.IsNullOrWhiteSpace(errorCode))
		{
			return $"/sign-in?returnUrl={Uri.EscapeDataString(returnUrl)}{usernameSegment}";
		}

		return $"/sign-in?returnUrl={Uri.EscapeDataString(returnUrl)}&error={Uri.EscapeDataString(errorCode)}{usernameSegment}";
	}

	private static ClaimsPrincipal CreatePrincipal(Domain.LocalUser user)
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

		var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme, ClaimTypes.Name, ClaimTypes.Role);
		return new ClaimsPrincipal(identity);
	}
}