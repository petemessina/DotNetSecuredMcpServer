using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;
using System.Security.Claims;

namespace SemanticKernelHandoffAgent.Providers;

internal sealed class MsalAuthorizationHeaderProvider (
    ITokenAcquisition tokenAcquisition
) : IAuthorizationHeaderProvider {

    public async Task<string> CreateAuthorizationHeaderAsync(
        IEnumerable<string> scopes, 
        AuthorizationHeaderProviderOptions? options = null, 
        ClaimsPrincipal? claimsPrincipal = null, 
        CancellationToken cancellationToken = default
    ) {
        var accessToken = await tokenAcquisition.GetAccessTokenForUserAsync(
            scopes, 
            user: claimsPrincipal, 
            tokenAcquisitionOptions: ConvertToTokenAcquisitionOptions(options?.AcquireTokenOptions)
        );

        return $"Bearer {accessToken}";
    }

    public async Task<string> CreateAuthorizationHeaderForAppAsync(
        string scopes, 
        AuthorizationHeaderProviderOptions? downstreamApiOptions = null, 
        CancellationToken cancellationToken = default
    ) {
        var accessToken = await tokenAcquisition.GetAccessTokenForAppAsync(
            scopes, 
            tokenAcquisitionOptions: ConvertToTokenAcquisitionOptions(downstreamApiOptions?.AcquireTokenOptions)
        );

        return $"Bearer {accessToken}";
    }

    public async Task<string> CreateAuthorizationHeaderForUserAsync(
        IEnumerable<string> scopes, 
        AuthorizationHeaderProviderOptions? authorizationHeaderProviderOptions = null, 
        ClaimsPrincipal? claimsPrincipal = null, 
        CancellationToken cancellationToken = default
    ) {
        var accessToken = await tokenAcquisition.GetAccessTokenForUserAsync(
            scopes, 
            user: claimsPrincipal, 
            tokenAcquisitionOptions: ConvertToTokenAcquisitionOptions(authorizationHeaderProviderOptions?.AcquireTokenOptions)
        );

        return $"Bearer {accessToken}";
    }

    private static TokenAcquisitionOptions? ConvertToTokenAcquisitionOptions(AcquireTokenOptions? acquireTokenOptions)
    {
        if (acquireTokenOptions == null)
        {
            return null;
        }

        return new TokenAcquisitionOptions
        {
            CorrelationId = acquireTokenOptions.CorrelationId,
            ExtraQueryParameters = acquireTokenOptions.ExtraQueryParameters,
            ForceRefresh = acquireTokenOptions.ForceRefresh,
            Claims = acquireTokenOptions.Claims,
            PopPublicKey = acquireTokenOptions.PopPublicKey,
            PopClaim = acquireTokenOptions.PopClaim,
            ManagedIdentity = acquireTokenOptions.ManagedIdentity
        };
    }
}
