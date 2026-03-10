using Kursa.Application.Features.Moodle.Commands;
using Kursa.Application.Features.Moodle.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kursa.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MoodleController(ISender sender) : ControllerBase
{
    [HttpGet("status")]
    public async Task<IActionResult> GetConnectionStatusAsync(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetMoodleConnectionStatusQuery(), cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(result.Error);
    }

    [HttpPost("link")]
    public async Task<IActionResult> LinkTokenAsync(
        LinkMoodleTokenCommand command,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);

        return result.IsSuccess
            ? Ok()
            : BadRequest(result.Error);
    }

    [HttpDelete("link")]
    public async Task<IActionResult> UnlinkTokenAsync(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new UnlinkMoodleTokenCommand(), cancellationToken);

        return result.IsSuccess
            ? Ok()
            : BadRequest(result.Error);
    }
}
