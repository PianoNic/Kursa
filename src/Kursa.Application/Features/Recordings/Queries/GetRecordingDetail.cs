using Kursa.Application.Common.Interfaces;
using Kursa.Application.Common.Models;
using Kursa.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Kursa.Application.Features.Recordings.Queries;

public sealed record GetRecordingDetailQuery(Guid RecordingId) : IRequest<Result<RecordingDetailDto>>;

public sealed class GetRecordingDetailHandler(
    ICurrentUserService currentUserService,
    IAppDbContext dbContext) : IRequestHandler<GetRecordingDetailQuery, Result<RecordingDetailDto>>
{
    public async Task<Result<RecordingDetailDto>> Handle(GetRecordingDetailQuery request, CancellationToken cancellationToken)
    {
        if (!currentUserService.IsAuthenticated || currentUserService.ExternalId is null)
            return Result<RecordingDetailDto>.Failure("User is not authenticated.");

        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.ExternalId == currentUserService.ExternalId, cancellationToken);

        if (user is null)
            return Result<RecordingDetailDto>.Failure("User not found.");

        Recording? recording = await dbContext.Recordings
            .Include(r => r.Course)
            .Include(r => r.Segments.OrderBy(s => s.OrderIndex))
            .FirstOrDefaultAsync(r => r.Id == request.RecordingId && r.UserId == user.Id, cancellationToken);

        if (recording is null)
            return Result<RecordingDetailDto>.Failure("Recording not found.");

        List<TranscriptSegmentDto> segments = recording.Segments
            .Select(s => new TranscriptSegmentDto(
                s.Id,
                s.OrderIndex,
                s.StartSeconds,
                s.EndSeconds,
                s.Text,
                s.Speaker))
            .ToList();

        return Result<RecordingDetailDto>.Success(new RecordingDetailDto(
            recording.Id,
            recording.Title,
            recording.Description,
            recording.CourseId,
            recording.Course?.Name,
            recording.FileName,
            recording.ContentType,
            recording.FileSizeBytes,
            recording.DurationSeconds,
            recording.Status,
            recording.TranscriptText,
            segments,
            recording.TranscribedAt,
            recording.ErrorMessage,
            recording.CreatedAt));
    }
}
