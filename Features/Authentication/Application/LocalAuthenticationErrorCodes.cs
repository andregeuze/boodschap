namespace Boodschap.Features.Authentication.Application;

public static class LocalAuthenticationErrorCodes
{
	public const string AdminRequired = "admin-required";
	public const string BootstrapRegistrationClosed = "bootstrap-registration-closed";
	public const string CurrentPasswordInvalid = "current-password-invalid";
	public const string CurrentPasswordRequired = "current-password-required";
	public const string InvalidCredentials = "invalid-credentials";
	public const string UsernameRequired = "username-required";
	public const string PasswordRequired = "password-required";
	public const string PasswordTooShort = "password-too-short";
	public const string PasswordMismatch = "password-mismatch";
	public const string UsernameTaken = "username-taken";
}