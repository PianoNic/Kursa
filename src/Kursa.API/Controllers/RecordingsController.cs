using Kursa.Application.Features.Recordings.Commands;
using Kursa.Application.Features.Recordings.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kursa.API.Controllers;

[ApiController]
[Route("api/recordings")]
[Authorize]
public class RecordingsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetRecordingsAsync(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetRecordingsQuery(), cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(result.Error);
    }

    [HttpGet("{recordingId:guid}")]
    public async Task<IActionResult> GetRecordingDetailAsync(Guid recordingId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetRecordingDetailQuery(recordingId), cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(result.Error);
    }

    [HttpGet("{recordingId:guid}/download-url")]
    public async Task<IActionResult> GetDownloadUrlAsync(Guid recordingId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetRecordingDownloadUrlQuery(recordingId), cancellationToken);

        return result.IsSuccess
            ? Ok(new { url = result.Value })
            : BadRequest(result.Error);
    }

    [HttpPost("upload")]
    [RequestSizeLimit(500 * 1024 * 1024)] // 500 MB
    public async Task<IActionResult> UploadRecordingAsync(
        [FromForm] string title,
        [FromForm] string? description,
        [FromForm] Guid? courseId,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
            return BadRequest("No file provided.");

        await using Stream stream = file.OpenReadStream();

        var command = new UploadRecordingCommand(
            title,
            description,
            courseId,
            file.FileName,
            file.ContentType,
            file.Length,
            stream);

        var result = await sender.Send(command, cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(result.Error);
    }

    [HttpPost("{recordingId:guid}/transcribe")]
    public async Task<IActionResult> TranscribeRecordingAsync(Guid recordingId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new TranscribeRecordingCommand(recordingId), cancellationToken);

        return result.IsSuccess
            ? Ok()
            : BadRequest(result.Error);
    }

    [HttpDelete("{recordingId:guid}")]
    public async Task<IActionResult> DeleteRecordingAsync(Guid recordingId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DeleteRecordingCommand(recordingId), cancellationToken);

        return result.IsSuccess
            ? Ok()
            : BadRequest(result.Error);
    }
}
