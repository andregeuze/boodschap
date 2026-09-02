namespace Boodschap.Mobile;

public sealed class BackendOptions
{
	public const string SectionName = "Backend";
	public const string HostedBaseUrl = "https://boodschap.geuze.dev/";
	public const string LocalSmokeTestBaseUrl = "http://127.0.0.1:5091/";

#if DEBUG
	public const string DefaultBaseUrl = LocalSmokeTestBaseUrl;
#else
	public const string DefaultBaseUrl = HostedBaseUrl;
#endif

	public string BaseUrl { get; set; } = DefaultBaseUrl;

	public Uri GetValidatedBaseUri()
	{
		if (!Uri.TryCreate(BaseUrl, UriKind.Absolute, out var uri))
		{
			throw new InvalidOperationException("The mobile backend URL must be an absolute URL.");
		}

		if (uri.Scheme == Uri.UriSchemeHttps)
		{
			return WithTrailingSlash(uri);
		}

#if DEBUG
		if (uri.Scheme == Uri.UriSchemeHttp && uri.IsLoopback && uri.Port == 5091)
		{
			return WithTrailingSlash(uri);
		}
#endif

		throw new InvalidOperationException("The mobile backend URL must use HTTPS.");
	}

	private static Uri WithTrailingSlash(Uri uri) =>
		uri.AbsoluteUri.EndsWith("/", StringComparison.Ordinal) ? uri : new Uri($"{uri.AbsoluteUri}/");
}