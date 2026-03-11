using Kursa.Application.Features.Quizzes.Commands;
using Kursa.Application.Features.Quizzes.Queries;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kursa.API.Controllers;

[ApiController]
[Route("api/quizzes")]
[Authorize]
public class QuizzesController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetQuizzesAsync(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetQuizzesQuery(), cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(result.Error);
    }

    [HttpGet("{quizId:guid}")]
    public async Task<IActionResult> GetQuizDetailAsync(Guid quizId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetQuizDetailQuery(quizId), cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : NotFound(result.Error);
    }

    [HttpPost("generate")]
    public async Task<IActionResult> GenerateQuizAsync([FromBody] GenerateQuizRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GenerateQuizCommand(
            request.ContentId,
            request.QuestionCount,
            request.Topic,
            request.DurationSeconds), cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(result.Error);
    }

    [HttpPost("{quizId:guid}/submit")]
    public async Task<IActionResult> SubmitAttemptAsync(Guid quizId, [FromBody] SubmitAttemptRequest request, CancellationToken cancellationToken)
    {
        var answers = request.Answers
            .Select(a => new AnswerSubmission(a.QuestionId, a.Answer))
            .ToList();

        var result = await sender.Send(new SubmitQuizAttemptCommand(quizId, answers, request.DurationSeconds), cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(result.Error);
    }

    [HttpGet("{quizId:guid}/results")]
    public async Task<IActionResult> GetQuizResultsAsync(Guid quizId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetQuizResultsQuery(quizId), cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : NotFound(result.Error);
    }

    [HttpGet("attempts/{attemptId:guid}")]
    public async Task<IActionResult> GetAttemptDetailAsync(Guid attemptId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetAttemptDetailQuery(attemptId), cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : NotFound(result.Error);
    }
}

public sealed record GenerateQuizRequest(
    Guid ContentId,
    int QuestionCount = 10,
    string? Topic = null,
    int DurationSeconds = 600);

public sealed record SubmitAttemptRequest(
    IReadOnlyList<AnswerSubmissionRequest> Answers,
    int DurationSeconds);

public sealed record AnswerSubmissionRequest(Guid QuestionId, string Answer);
