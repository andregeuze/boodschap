using Boodschap.Features.Authentication.Infrastructure;
using Boodschap.Features.Authentication.Infrastructure.Persistence;
using Boodschap.Features.Authentication.Presentation;
using Boodschap.Shared.Infrastructure.Authentication;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

namespace Boodschap.Features.Authentication;

public static class AuthenticationModule
{
	public static IServiceCollection AddAuthenticationFeature(this IServiceCollection services, string sqliteConnectionString)
	{
		services.AddLocalAuthenticationCore(sqliteConnectionString);
		services.AddDataProtection()
			.SetApplicationName(LocalAuthenticationDefaults.DataProtectionApplicationName)
			.PersistKeysToDbContext<AuthenticationDbContext>();
		services.AddCascadingAuthenticationState();
		services.AddAuthorization();
		services.AddHttpContextAccessor();
		services.AddRateLimiter(options =>
		{
			options.AddPolicy(LocalAuthenticationDefaults.MobileAuthenticationRateLimitPolicy, httpContext =>
				RateLimitPartition.GetFixedWindowLimiter(
					httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
					_ => new FixedWindowRateLimiterOptions
					{
						PermitLimit = 10,
						Window = TimeSpan.FromMinutes(1),
						QueueLimit = 0
					}));
		});

		services.AddAuthentication(options =>
			{
				options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
				options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
				options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
				options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
			})
			.AddCookie(options =>
			{
				options.LoginPath = "/sign-in";
				options.AccessDeniedPath = "/sign-in";
				options.LogoutPath = "/account/logout";
				options.ExpireTimeSpan = LocalAuthenticationDefaults.PersistentSignInLifetime;
				options.SlidingExpiration = true;
			})
			.AddBearerToken(ApiAuthenticationDefaults.BearerScheme, options =>
			{
				options.BearerTokenExpiration = LocalAuthenticationDefaults.BearerTokenLifetime;
				options.RefreshTokenExpiration = LocalAuthenticationDefaults.RefreshTokenLifetime;
			});

		return services;
	}

	public static IEndpointRouteBuilder MapAuthenticationFeature(this IEndpointRouteBuilder endpoints)
	{
		endpoints.MapAuthenticationEndpoints();
		endpoints.MapAuthenticationApiEndpoints();
		return endpoints;
	}
}