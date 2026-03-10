using Kursa.Application.Features.Analytics;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kursa.API.Controllers;

[ApiController]
[Route("api/analytics")]
[Authorize]
public class AnalyticsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAnalyticsAsync(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetAnalyticsQuery(), cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(result.Error);
    }
}
