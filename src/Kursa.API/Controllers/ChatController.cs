using Kursa.Application.Features.Chat.Commands;
using Kursa.Application.Features.Chat.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kursa.API.Controllers;

[ApiController]
[Route("api/chat")]
[Authorize]
public class ChatController(ISender sender) : ControllerBase
{
    [HttpGet("threads")]
    public async Task<IActionResult> GetThreadsAsync(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetChatThreadsQuery(), cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(result.Error);
    }

    [HttpGet("threads/{threadId:guid}/messages")]
    public async Task<IActionResult> GetMessagesAsync(Guid threadId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetChatMessagesQuery(threadId), cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : NotFound(result.Error);
    }

    [HttpPost("send")]
    public async Task<IActionResult> SendMessageAsync([FromBody] SendChatRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new SendChatMessageCommand(request.ThreadId, request.Message), cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(result.Error);
    }
}

public sealed record SendChatRequest(Guid? ThreadId, string Message);
