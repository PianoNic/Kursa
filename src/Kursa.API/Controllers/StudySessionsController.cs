using Kursa.Application.Features.StudySessions;
using Kursa.Application.Features.StudySessions.Commands;
using Kursa.Application.Features.StudySessions.Queries;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kursa.API.Controllers;

[ApiController]
[Route("api/study-sessions")]
[Authorize]
public class StudySessionsController(ISender sender) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<StudySessionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSessionsAsync(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetStudySessionsQuery(), cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(result.Error);
    }

    [HttpPost("start")]
    [ProducesResponseType(typeof(StudySessionDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> StartSessionAsync([FromBody] StartSessionRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new StartStudySessionCommand(
            request.Title,
            request.WorkMinutes,
            request.BreakMinutes), cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(result.Error);
    }

    [HttpPost("{sessionId:guid}/complete")]
    [ProducesResponseType(typeof(StudySessionDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> CompleteSessionAsync(Guid sessionId, [FromBody] CompleteSessionRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CompleteStudySessionCommand(
            sessionId,
            request.CompletedPomodoros,
            request.TotalDurationSeconds,
            request.CardsReviewed,
            request.QuizQuestionsAnswered,
            request.QuizCorrectAnswers), cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(result.Error);
    }
}

public sealed record StartSessionRequest(
    string Title,
    int WorkMinutes = 25,
    int BreakMinutes = 5);

public sealed record CompleteSessionRequest(
    int CompletedPomodoros,
    int TotalDurationSeconds,
    int CardsReviewed,
    int QuizQuestionsAnswered,
    int QuizCorrectAnswers);
