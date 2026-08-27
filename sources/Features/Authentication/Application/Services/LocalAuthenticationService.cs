using Boodschap.Features.Authentication.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;

namespace Boodschap.Features.Authentication.Application;

public sealed class LocalAuthenticationService(
	ILocalUserRepository localUserRepository,
	IPasswordHasher<LocalUser> passwordHasher) : ILocalAuthenticationService
{
	private const int MinimumPasswordLength = 8;

	public async Task<LocalAuthenticationResult> LoginAsync(string username, string password, CancellationToken cancellationToken = default)
	{
		var normalizedUsername = NormalizeUsername(username);
		if (string.IsNullOrWhiteSpace(normalizedUsername))
		{
			return LocalAuthenticationResult.Failure(LocalAuthenticationErrorCodes.UsernameRequired);
		}

		if (string.IsNullOrWhiteSpace(password))
		{
			return LocalAuthenticationResult.Failure(LocalAuthenticationErrorCodes.PasswordRequired);
		}

		var user = await localUserRepository.GetByNormalizedUsernameAsync(normalizedUsername, cancellationToken);
		if (user is null)
		{
			return LocalAuthenticationResult.Failure(LocalAuthenticationErrorCodes.InvalidCredentials);
		}

		var verification = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
		if (verification == PasswordVerificationResult.Failed)
		{
			return LocalAuthenticationResult.Failure(LocalAuthenticationErrorCodes.InvalidCredentials);
		}

		return LocalAuthenticationResult.Success(user);
	}

	public async Task<bool> IsBootstrapRegistrationOpenAsync(CancellationToken cancellationToken = default)
	{
		return await localUserRepository.GetUserCountAsync(cancellationToken) == 0;
	}

	public async Task<LocalAuthenticationResult> RegisterAsync(string username, string password, string confirmPassword, CancellationToken cancellationToken = default)
	{
		if (!await IsBootstrapRegistrationOpenAsync(cancellationToken))
		{
			return LocalAuthenticationResult.Failure(LocalAuthenticationErrorCodes.BootstrapRegistrationClosed);
		}

		return await CreateUserInternalAsync(username, password, confirmPassword, isAdmin: true, cancellationToken);
	}

	public async Task<LocalAuthenticationResult> CreateUserAsync(int actorUserId, string username, string password, string confirmPassword, bool isAdmin, CancellationToken cancellationToken = default)
	{
		var actor = await localUserRepository.GetByIdAsync(actorUserId, cancellationToken);
		if (actor is null || !actor.IsAdmin)
		{
			return LocalAuthenticationResult.Failure(LocalAuthenticationErrorCodes.AdminRequired);
		}

		return await CreateUserInternalAsync(username, password, confirmPassword, isAdmin, cancellationToken);
	}

	public async Task<LocalPasswordChangeResult> ChangePasswordAsync(int actorUserId, string currentPassword, string newPassword, string confirmPassword, CancellationToken cancellationToken = default)
	{
		var actor = await localUserRepository.GetByIdAsync(actorUserId, cancellationToken);
		if (actor is null)
		{
			return LocalPasswordChangeResult.Failure(LocalAuthenticationErrorCodes.CurrentPasswordInvalid);
		}

		if (string.IsNullOrWhiteSpace(currentPassword))
		{
			return LocalPasswordChangeResult.Failure(LocalAuthenticationErrorCodes.CurrentPasswordRequired);
		}

		var verification = passwordHasher.VerifyHashedPassword(actor, actor.PasswordHash, currentPassword);
		if (verification == PasswordVerificationResult.Failed)
		{
			return LocalPasswordChangeResult.Failure(LocalAuthenticationErrorCodes.CurrentPasswordInvalid);
		}

		var passwordValidationError = ValidatePassword(password: newPassword, confirmPassword);
		if (passwordValidationError is not null)
		{
			return LocalPasswordChangeResult.Failure(passwordValidationError);
		}

		actor.PasswordHash = passwordHasher.HashPassword(actor, newPassword);
		await localUserRepository.UpdatePasswordHashAsync(actor.Id, actor.PasswordHash, cancellationToken);

		return LocalPasswordChangeResult.Success();
	}

	private async Task<LocalAuthenticationResult> CreateUserInternalAsync(string username, string password, string confirmPassword, bool isAdmin, CancellationToken cancellationToken)
	{
		var trimmedUsername = username.Trim();
		var normalizedUsername = NormalizeUsername(username);
		if (string.IsNullOrWhiteSpace(normalizedUsername))
		{
			return LocalAuthenticationResult.Failure(LocalAuthenticationErrorCodes.UsernameRequired);
		}

		var passwordValidationError = ValidatePassword(password, confirmPassword);
		if (passwordValidationError is not null)
		{
			return LocalAuthenticationResult.Failure(passwordValidationError);
		}

		if (await localUserRepository.GetByNormalizedUsernameAsync(normalizedUsername, cancellationToken) is not null)
		{
			return LocalAuthenticationResult.Failure(LocalAuthenticationErrorCodes.UsernameTaken);
		}

		var user = new LocalUser
		{
			Username = trimmedUsername,
			NormalizedUsername = normalizedUsername,
			IsAdmin = isAdmin,
			CreatedUtc = DateTimeOffset.UtcNow
		};
		user.PasswordHash = passwordHasher.HashPassword(user, password);

		try
		{
			var createdUser = await localUserRepository.CreateAsync(user, cancellationToken);
			return LocalAuthenticationResult.Success(createdUser);
		}
		catch (SqliteException exception) when (exception.SqliteErrorCode == 19)
		{
			return LocalAuthenticationResult.Failure(LocalAuthenticationErrorCodes.UsernameTaken);
		}
	}

	private static string? ValidatePassword(string password, string confirmPassword)
	{
		if (string.IsNullOrWhiteSpace(password))
		{
			return LocalAuthenticationErrorCodes.PasswordRequired;
		}

		if (password.Length < MinimumPasswordLength)
		{
			return LocalAuthenticationErrorCodes.PasswordTooShort;
		}

		if (!string.Equals(password, confirmPassword, StringComparison.Ordinal))
		{
			return LocalAuthenticationErrorCodes.PasswordMismatch;
		}

		return null;
	}

	private static string NormalizeUsername(string username)
	{
		return username.Trim().ToUpperInvariant();
	}
}