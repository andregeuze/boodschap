namespace Boodschap.Features.Authentication.Application;

public sealed record AuthenticationLoginRequest(string Username, string Password);

public sealed record AuthenticationRefreshRequest(string RefreshToken);

public sealed record AuthenticationChangePasswordRequest(string CurrentPassword, string NewPassword, string ConfirmPassword);

public sealed record AuthenticationCreateUserRequest(string Username, string Password, string ConfirmPassword, bool IsAdmin);

public sealed record AuthenticationUserResponse(int Id, string Username, bool IsAdmin);

public sealed record AuthenticationErrorResponse(string Code);

public sealed record AuthenticationTokenResponse(
	string TokenType,
	string AccessToken,
	long ExpiresIn,
	string RefreshToken);