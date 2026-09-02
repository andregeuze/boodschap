using Boodschap.Features.Authentication.Application;
using System.Net;
using System.Net.Http.Headers;

namespace Boodschap.Mobile;

public sealed class AuthenticatedHttpMessageHandler(
	IRemoteAuthenticationClient authenticationClient,
	MobileSessionState sessionState) : DelegatingHandler
{
	protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		var retryRequest = await CloneAsync(request, cancellationToken);
		var accessToken = await authenticationClient.GetAccessTokenAsync(cancellationToken);
		if (!string.IsNullOrWhiteSpace(accessToken))
		{
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
		}

		var response = await base.SendAsync(request, cancellationToken);
		if (response.StatusCode != HttpStatusCode.Unauthorized)
		{
			retryRequest.Dispose();
			return response;
		}

		if (!await authenticationClient.RefreshAsync(cancellationToken))
		{
			retryRequest.Dispose();
			await sessionState.SetAnonymousAsync();
			return response;
		}

		response.Dispose();
		accessToken = await authenticationClient.GetAccessTokenAsync(cancellationToken);
		retryRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
		return await base.SendAsync(retryRequest, cancellationToken);
	}

	private static async Task<HttpRequestMessage> CloneAsync(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		var clone = new HttpRequestMessage(request.Method, request.RequestUri)
		{
			Version = request.Version,
			VersionPolicy = request.VersionPolicy
		};

		foreach (var header in request.Headers)
		{
			clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
		}

		if (request.Content is not null)
		{
			var content = new ByteArrayContent(await request.Content.ReadAsByteArrayAsync(cancellationToken));
			foreach (var header in request.Content.Headers)
			{
				content.Headers.TryAddWithoutValidation(header.Key, header.Value);
			}
			clone.Content = content;
		}

		return clone;
	}
}