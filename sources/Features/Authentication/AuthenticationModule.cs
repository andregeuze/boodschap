using Boodschap.Features.Authentication.Application;
using Boodschap.Features.Authentication.Domain;
using Boodschap.Features.Authentication.Infrastructure;
using Boodschap.Features.Authentication.Infrastructure.Persistence;
using Boodschap.Features.Authentication.Presentation;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Boodschap.Features.Authentication;

public static class AuthenticationModule
{
	public static IServiceCollection AddAuthenticationFeature(this IServiceCollection services, string sqliteConnectionString)
	{
		services.AddDbContextFactory<AuthenticationDbContext>(options => options.UseSqlite(
			sqliteConnectionString,
			sqlite => sqlite.MigrationsHistoryTable(AuthenticationDbContext.MigrationsHistoryTableName)));
		services.AddDataProtection()
			.SetApplicationName(LocalAuthenticationDefaults.DataProtectionApplicationName)
			.PersistKeysToDbContext<AuthenticationDbContext>();
		services.AddCascadingAuthenticationState();
		services.AddAuthorization();
		services.AddHttpContextAccessor();
		services.AddScoped<ICurrentUserAccessor, AuthenticationStateCurrentUserAccessor>();
		services.AddScoped<ILocalAuthenticationService, LocalAuthenticationService>();
		services.AddScoped<ILocalUserRepository, LocalUserRepository>();
		services.AddScoped<IPasswordHasher<LocalUser>, PasswordHasher<LocalUser>>();

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
			});

		return services;
	}

	public static IEndpointRouteBuilder MapAuthenticationFeature(this IEndpointRouteBuilder endpoints)
	{
		endpoints.MapAuthenticationEndpoints();
		return endpoints;
	}
}