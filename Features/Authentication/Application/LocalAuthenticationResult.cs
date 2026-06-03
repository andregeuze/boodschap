using Boodschap.Features.Authentication.Domain;

namespace Boodschap.Features.Authentication.Application;

public sealed record LocalAuthenticationResult(LocalUser? User, string? ErrorCode)
{
	public bool Succeeded => User is not null && string.IsNullOrWhiteSpace(ErrorCode);

	public static LocalAuthenticationResult Success(LocalUser user)
	{
		return new(user, null);
	}

	public static LocalAuthenticationResult Failure(string errorCode)
	{
		return new(null, errorCode);
	}
}