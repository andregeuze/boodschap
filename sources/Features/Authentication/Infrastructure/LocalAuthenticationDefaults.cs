namespace Boodschap.Features.Authentication.Infrastructure;

public static class LocalAuthenticationDefaults
{
	public const string AdminRole = "Admin";
	public const string Issuer = "boodschap-local";
	public const string DataProtectionApplicationName = "Boodschap";
	public const string MobileAuthenticationRateLimitPolicy = "mobile-authentication";
	public static readonly TimeSpan PersistentSignInLifetime = TimeSpan.FromDays(90);
	public static readonly TimeSpan BearerTokenLifetime = TimeSpan.FromHours(1);
	public static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(30);
}