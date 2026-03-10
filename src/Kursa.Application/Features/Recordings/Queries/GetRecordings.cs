using Kursa.Application.Common.Interfaces;
using Kursa.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Kursa.Application.Features.Recordings.Queries;

public sealed record GetRecordingsQuery : IRequest<Result<IReadOnlyList<RecordingDto>>>;

public sealed class GetRecordingsHandler(
    ICurrentUserService currentUserService,
    IAppDbContext dbContext) : IRequestHandler<GetRecordingsQuery, Result<IReadOnlyList<RecordingDto>>>
{
    public async Task<Result<IReadOnlyList<RecordingDto>>> Handle(GetRecordingsQuery request, CancellationToken cancellationToken)
    {
        if (!currentUserService.IsAuthenticated || currentUserService.ExternalId is null)
            return Result<IReadOnlyList<RecordingDto>>.Failure("User is not authenticated.");

        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.ExternalId == currentUserService.ExternalId, cancellationToken);

        if (user is null)
            return Result<IReadOnlyList<RecordingDto>>.Failure("User not found.");

        List<RecordingDto> recordings = await dbContext.Recordings
            .Where(r => r.UserId == user.Id)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new RecordingDto(
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
                r.TranscriptText != null,
                r.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<RecordingDto>>.Success(recordings);
    }
}
