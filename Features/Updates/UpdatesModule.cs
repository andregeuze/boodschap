using System.Net.Http.Headers;
using Boodschap.Features.Updates.Application.Contracts;
using Boodschap.Features.Updates.Infrastructure;

namespace Boodschap.Features.Updates;

public static class UpdatesModule
{
	public const string HttpClientName = "GitHubUpdates";

	public static IServiceCollection AddUpdatesFeature(this IServiceCollection services, IConfiguration configuration)
	{
		services.Configure<UpdateFeatureOptions>(configuration.GetSection(UpdateFeatureOptions.SectionName));

		if (!configuration.IsUpdatesFeatureEnabled())
		{
			return services;
		}

		services.AddHttpClient(HttpClientName, client =>
		{
			client.BaseAddress = new Uri("https://api.github.com/");
			client.Timeout = TimeSpan.FromSeconds(10);
			client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Boodschap", "1.0"));
			client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
		});
		services.AddSingleton<IUpdateCheckService, GitHubUpdateCheckService>();
		services.AddHostedService<UpdateCheckBackgroundService>();

		return services;
	}

	public static bool IsUpdatesFeatureEnabled(this IConfiguration configuration)
	{
		return configuration.GetValue($"{UpdateFeatureOptions.SectionName}:Enabled", true);
	}
}