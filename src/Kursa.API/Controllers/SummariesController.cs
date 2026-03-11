using Kursa.Application.Features.Summaries.Commands;
using Kursa.Application.Features.Summaries.Queries;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kursa.API.Controllers;

[ApiController]
[Route("api/summaries")]
[Authorize]
public class SummariesController(ISender sender) : ControllerBase
{
    [HttpGet("{contentId:guid}")]
    public async Task<IActionResult> GetSummaryAsync(Guid contentId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetContentSummaryQuery(contentId), cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : NotFound(result.Error);
    }

    [HttpPost("{contentId:guid}")]
    public async Task<IActionResult> GenerateSummaryAsync(Guid contentId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GenerateSummaryCommand(contentId), cancellationToken);

        return result.IsSuccess
            ? Ok(new { summary = result.Value })
            : BadRequest(result.Error);
    }
}
