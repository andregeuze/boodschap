namespace Boodschap.Features.Authentication.Application;

public sealed record CurrentUser(int LocalUserId, string Id, string DisplayName, string? Email, bool IsAdmin);