using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Kursa.API.DevAuth;

/// <summary>
/// Used only in Development when no OIDC authority is configured.
/// Injects a fixed dev user so all API endpoints work without a real identity provider.
/// </summary>
public sealed class DevAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "DevAuth";
    public const string DevUserId = "dev-user-1";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[]
        {
            new Claim("sub", DevUserId),
            new Claim(ClaimTypes.NameIdentifier, DevUserId),
            new Claim(ClaimTypes.Name, "Dev User"),
            new Claim(ClaimTypes.Email, "dev@kursa.local"),
        };

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
