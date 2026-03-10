using Kursa.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kursa.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AuthController(
    ICurrentUserService currentUserService,
    IUserSyncService userSyncService) : ControllerBase
{
    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUserAsync(CancellationToken cancellationToken)
    {
        if (currentUserService.ExternalId is null || currentUserService.Email is null)
        {
            return Unauthorized();
        }

        var user = await userSyncService.SyncUserAsync(
            currentUserService.ExternalId,
            currentUserService.Email,
            currentUserService.DisplayName ?? currentUserService.Email,
            cancellationToken);

        return Ok(new
        {
            user.Id,
            user.Email,
            user.DisplayName,
            user.Role,
            user.OnboardingCompleted,
            user.MoodleUrl,
            HasMoodleToken = user.MoodleToken is not null
        });
    }
}
