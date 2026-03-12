namespace Kursa.API.Dtos;

public sealed record AppInfoResponse(
    string Version,
    string Environment,
    bool IsHealthy,
    OidcConfigResponse Oidc);

public sealed record OidcConfigResponse(
    string Issuer,
    string ClientId,
    string RedirectUri,
    string PostLogoutRedirectUri,
    string Scope);
