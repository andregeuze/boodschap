using Boodschap.Features.Authentication.Application;
using Boodschap.Features.Authentication.Domain;
using Boodschap.Features.Authentication.Infrastructure;
using Boodschap.Features.Authentication.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Boodschap.Features.Authentication;

public static class LocalAuthenticationModule
{
	public static IServiceCollection AddLocalAuthenticationCore(this IServiceCollection services, string sqliteConnectionString)
	{
		services.AddDbContextFactory<AuthenticationDbContext>(options => options.UseSqlite(
			sqliteConnectionString,
			sqlite => sqlite.MigrationsHistoryTable(AuthenticationDbContext.MigrationsHistoryTableName)));
		services.AddScoped<ICurrentUserAccessor, AuthenticationStateCurrentUserAccessor>();
		services.AddScoped<ILocalAuthenticationService, LocalAuthenticationService>();
		services.AddScoped<ILocalUserRepository, LocalUserRepository>();
		services.AddScoped<IPasswordHasher<LocalUser>, PasswordHasher<LocalUser>>();

		return services;
	}
}