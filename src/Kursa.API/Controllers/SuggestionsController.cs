using Kursa.Application.Features.Suggestions.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kursa.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SuggestionsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetSuggestionsAsync(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetStudySuggestionsQuery(), cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(result.Error);
    }
}
