namespace Boodschap.Features.Authentication.Domain;

public sealed class LocalUser
{
	public int Id { get; set; }
	public string Username { get; set; } = string.Empty;
	public string NormalizedUsername { get; set; } = string.Empty;
	public string PasswordHash { get; set; } = string.Empty;
	public bool IsAdmin { get; set; }
	public DateTimeOffset CreatedUtc { get; set; }
}