namespace Boodschap.Features.Recipes.Infrastructure.Integration;

public sealed class N8nRecipeSearchOptions
{
	public const string SectionName = "Recipes:N8n";

	public string WebhookUrl { get; init; } = string.Empty;
	public int TimeoutSeconds { get; init; } = 180;

	public TimeSpan GetTimeout()
	{
		return TimeSpan.FromSeconds(Math.Clamp(TimeoutSeconds, 10, 600));
	}
}