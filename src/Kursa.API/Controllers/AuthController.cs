using System.Net.Http.Headers;
using System.Text.Json;
using Kursa.Application.Common.Interfaces;
using Kursa.Application.Features.Users;
using Kursa.Domain.Enums;
using Kursa.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kursa.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AuthController(
    ICurrentUserService currentUserService,
    IUserSyncService userSyncService,
    IAppDbContext dbContext,
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration) : ControllerBase
{
    /// <summary>
    /// Returns the current user profile. Returns 404 if the user hasn't completed registration yet.
    /// </summary>
    /// <summary>
    /// Returns the current user profile. Returns 404 if the user hasn't completed registration yet.
    /// </summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCurrentUserAsync(CancellationToken cancellationToken)
    {
        string? externalId = currentUserService.ExternalId;
        if (externalId is null)
            return Unauthorized();

        var user = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.ExternalId == externalId, cancellationToken);

        if (user is null)
            return NotFound(new { message = "User not registered." });

        return Ok(new UserDto(
            user.Id,
            user.Email,
            user.DisplayName,
            user.AvatarUrl,
            user.Role,
            user.OnboardingCompleted,
            user.MoodleUrl,
            user.MoodleToken is not null,
            user.CreatedAt));
    }

    /// <summary>
    /// Creates the user record in the DB after onboarding is complete.
    /// Fetches profile info from the OIDC userinfo endpoint if not available in the token.
    /// </summary>
    /// <summary>
    /// Creates the user record in the DB after onboarding is complete.
    /// Fetches profile info from the OIDC userinfo endpoint if not available in the token.
    /// </summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> RegisterAsync(CancellationToken cancellationToken)
    {
        string? externalId = currentUserService.ExternalId;
        if (externalId is null)
            return Unauthorized();

        // Get email/name from token claims, fall back to userinfo endpoint
        string? email = currentUserService.Email;
        string? displayName = currentUserService.DisplayName;

        if (string.IsNullOrEmpty(email))
        {
            var userInfo = await FetchUserInfoAsync(cancellationToken);
            email = userInfo?.Email ?? $"{externalId}@unknown";
            displayName ??= userInfo?.Name ?? email;
        }

        var user = await userSyncService.SyncUserAsync(
            externalId, email, displayName ?? email, cancellationToken);

        user.OnboardingCompleted = true;
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new UserDto(
            user.Id,
            user.Email,
            user.DisplayName,
            user.AvatarUrl,
            user.Role,
            user.OnboardingCompleted,
            user.MoodleUrl,
            user.MoodleToken is not null,
            user.CreatedAt));
    }

    private async Task<OidcUserInfo?> FetchUserInfoAsync(CancellationToken cancellationToken)
    {
        string? authority = configuration["Oidc:Authority"];
        if (string.IsNullOrEmpty(authority)) return null;

        // Extract the Bearer token from the current request
        string? accessToken = HttpContext.Request.Headers.Authorization
            .ToString().Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase).Trim();
        if (string.IsNullOrEmpty(accessToken)) return null;

        var client = httpClientFactory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, $"{authority.TrimEnd('/')}/api/oidc/userinfo");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await client.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode) return null;

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<OidcUserInfo>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
    }

    private sealed record OidcUserInfo(string? Sub, string? Email, string? Name);
}
