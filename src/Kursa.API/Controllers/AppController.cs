using System.Reflection;
using Kursa.API.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Kursa.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AppController(IWebHostEnvironment environment, IConfiguration configuration) : ControllerBase
{
    /// <summary>
    /// Returns application info: version, environment, and OIDC configuration.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(AppInfoResponse), StatusCodes.Status200OK)]
    public IActionResult GetAppInfo()
    {
        string version = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? Assembly.GetExecutingAssembly().GetName().Version?.ToString()
            ?? "unknown";

        var response = new AppInfoResponse(
            Version: version != "unknown" ? $"v{version}" : version,
            Environment: environment.IsProduction() ? "Production" : "Development",
            IsHealthy: true,
            Oidc: new OidcConfigResponse(
                Issuer: configuration["Oidc:Authority"] ?? "",
                ClientId: configuration["Oidc:ClientId"] ?? "",
                RedirectUri: configuration["Oidc:RedirectUri"] ?? "/callback",
                PostLogoutRedirectUri: configuration["Oidc:PostLogoutRedirectUri"] ?? "/",
                Scope: configuration["Oidc:Scope"] ?? "openid profile email"
            )
        );

        return Ok(response);
    }
}
