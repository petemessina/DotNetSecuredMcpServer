using Microsoft.Identity.Abstractions;
using SemanticKernelHandoffAgent.Models;
using System.Net.Http.Headers;

namespace SemanticKernelHandoffAgent.DelegatedHandlers;

internal sealed class DownstreamAuthorizationDelegatingHandler(
    IAuthorizationHeaderProvider authorizationHeaderProvider,
    ApplicationSettings applicationSettings
) : DelegatingHandler {
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    ) {
        string authorizationHeader = await authorizationHeaderProvider.CreateAuthorizationHeaderAsync(
            applicationSettings.EntraOauthConfiguration.Scopes,
            cancellationToken: cancellationToken
        );

        if (!string.IsNullOrEmpty(authorizationHeader))
        {
            request.Headers.Authorization = AuthenticationHeaderValue.Parse(authorizationHeader);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
