using Kursa.Application.Common.Interfaces;
using Kursa.Application.Common.Models;
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

        RecordingDetailDto? recording = await dbContext.Recordings
            .Where(r => r.Id == request.RecordingId && r.UserId == user.Id)
            .Select(r => new RecordingDetailDto(
                r.Id,
                r.Title,
                r.Description,
                r.CourseId,
                r.Course != null ? r.Course.Name : null,
                r.FileName,
                r.ContentType,
                r.FileSizeBytes,
                r.DurationSeconds,
                r.Status,
                r.TranscriptText,
                r.TranscribedAt,
                r.ErrorMessage,
                r.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        if (recording is null)
            return Result<RecordingDetailDto>.Failure("Recording not found.");

        return Result<RecordingDetailDto>.Success(recording);
    }
}
