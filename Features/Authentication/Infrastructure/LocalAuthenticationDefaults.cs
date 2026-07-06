namespace Boodschap.Features.Authentication.Infrastructure;

public static class LocalAuthenticationDefaults
{
	public const string AdminRole = "Admin";
	public const string Issuer = "boodschap-local";
	public const string DataProtectionApplicationName = "Boodschap";
	public static readonly TimeSpan PersistentSignInLifetime = TimeSpan.FromDays(90);
}