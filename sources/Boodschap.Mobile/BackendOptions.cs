namespace Boodschap.Mobile;

public sealed class BackendOptions
{
	public const string SectionName = "Backend";
	public const string DefaultBaseUrl = "https://boodschap.geuze.dev/";

	public string BaseUrl { get; set; } = DefaultBaseUrl;

	public Uri GetValidatedBaseUri()
	{
		if (!Uri.TryCreate(BaseUrl, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
		{
			throw new InvalidOperationException("The mobile backend URL must be an absolute HTTPS URL.");
		}

		return uri.AbsoluteUri.EndsWith("/", StringComparison.Ordinal) ? uri : new Uri($"{uri.AbsoluteUri}/");
	}
}