using Boodschap.Features.Updates.Application.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Boodschap.Features.Updates.Tests;

public sealed class UpdatesModuleTests
{
	[Fact]
	public void AddUpdatesFeature_WhenDisabled_DoesNotRegisterUpdateCheckService()
	{
		var services = new ServiceCollection();

		services.AddUpdatesFeature(CreateConfiguration(isEnabled: false));

		Assert.DoesNotContain(services, service => service.ServiceType == typeof(IUpdateCheckService));
	}

	[Fact]
	public void AddUpdatesFeature_WhenEnabled_RegistersUpdateCheckService()
	{
		var services = new ServiceCollection();

		services.AddUpdatesFeature(CreateConfiguration(isEnabled: true));

		Assert.Contains(services, service => service.ServiceType == typeof(IUpdateCheckService));
	}

	private static IConfiguration CreateConfiguration(bool isEnabled)
	{
		return new ConfigurationBuilder()
			.AddInMemoryCollection(new Dictionary<string, string?>
			{
				[$"{UpdateFeatureOptions.SectionName}:Enabled"] = isEnabled.ToString()
			})
			.Build();
	}
}