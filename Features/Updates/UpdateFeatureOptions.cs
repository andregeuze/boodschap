namespace Boodschap.Features.Updates;

public sealed class UpdateFeatureOptions
{
	public const string SectionName = "Features:Updates";

	public bool Enabled { get; set; } = true;

	public string Owner { get; set; } = "andregeuze";

	public string Repository { get; set; } = "boodschap";

	public string Branch { get; set; } = "main";

	public string? CurrentCommit { get; set; }

	public int CacheDurationMinutes { get; set; } = 15;
}