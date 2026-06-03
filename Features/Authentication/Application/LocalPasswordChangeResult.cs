namespace Boodschap.Features.Authentication.Application;

public sealed record LocalPasswordChangeResult(string? ErrorCode)
{
	public bool Succeeded => string.IsNullOrWhiteSpace(ErrorCode);

	public static LocalPasswordChangeResult Success()
	{
		return new(ErrorCode: null);
	}

	public static LocalPasswordChangeResult Failure(string errorCode)
	{
		return new(errorCode);
	}
}