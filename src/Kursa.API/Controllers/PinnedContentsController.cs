using Kursa.Application.Features.PinnedContents.Commands;
using Kursa.Application.Features.PinnedContents.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kursa.API.Controllers;

[ApiController]
[Route("api/pinned")]
[Authorize]
public class PinnedContentsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetPinnedContentsAsync(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetPinnedContentsQuery(), cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(result.Error);
    }

    [HttpPost("{contentId:guid}")]
    public async Task<IActionResult> PinContentAsync(Guid contentId, [FromBody] PinContentRequest? request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new PinContentCommand(contentId, request?.Notes), cancellationToken);

        return result.IsSuccess
            ? Ok(new { id = result.Value })
            : BadRequest(result.Error);
    }

    [HttpDelete("{contentId:guid}")]
    public async Task<IActionResult> UnpinContentAsync(Guid contentId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new UnpinContentCommand(contentId), cancellationToken);

        return result.IsSuccess
            ? Ok()
            : BadRequest(result.Error);
    }

    [HttpPost("{contentId:guid}/star")]
    public async Task<IActionResult> ToggleStarAsync(Guid contentId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ToggleStarCommand(contentId), cancellationToken);

        return result.IsSuccess
            ? Ok(new { isStarred = result.Value })
            : BadRequest(result.Error);
    }

    [HttpPost("{contentId:guid}/index")]
    public async Task<IActionResult> IndexContentAsync(Guid contentId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new IndexPinnedContentCommand(contentId), cancellationToken);

        return result.IsSuccess
            ? Ok()
            : BadRequest(result.Error);
    }
}

public sealed record PinContentRequest(string? Notes);
