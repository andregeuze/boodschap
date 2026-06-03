using Boodschap.Features.Authentication.Application;
using Boodschap.Features.Authentication.Domain;
using Boodschap.Features.Authentication.Infrastructure;
using Boodschap.Features.Authentication.Infrastructure.Persistence;
using Boodschap.Features.Authentication.Presentation;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;

namespace Boodschap.Features.Authentication;

public static class AuthenticationModule
{
	public static IServiceCollection AddAuthenticationFeature(this IServiceCollection services, string sqliteConnectionString)
	{
		services.AddCascadingAuthenticationState();
		services.AddAuthorization();
		services.AddHttpContextAccessor();
		services.AddScoped<ICurrentUserAccessor, AuthenticationStateCurrentUserAccessor>();
		services.AddScoped<ILocalAuthenticationService, LocalAuthenticationService>();
		services.AddScoped<ILocalUserRepository, SqliteLocalUserRepository>();
		services.AddScoped<IPasswordHasher<LocalUser>, PasswordHasher<LocalUser>>();
		services.AddSingleton(new AuthenticationStoreConfiguration(sqliteConnectionString));

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
			});

		return services;
	}

	public static IEndpointRouteBuilder MapAuthenticationFeature(this IEndpointRouteBuilder endpoints)
	{
		endpoints.MapAuthenticationEndpoints();
		return endpoints;
	}
}