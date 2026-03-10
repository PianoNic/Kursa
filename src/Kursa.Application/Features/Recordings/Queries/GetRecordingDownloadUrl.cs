using Kursa.Application.Common.Interfaces;
using Kursa.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Kursa.Application.Features.Recordings.Queries;

public sealed record GetRecordingDownloadUrlQuery(Guid RecordingId) : IRequest<Result<string>>;

public sealed class GetRecordingDownloadUrlHandler(
    ICurrentUserService currentUserService,
    IAppDbContext dbContext,
    IFileStorageService fileStorage) : IRequestHandler<GetRecordingDownloadUrlQuery, Result<string>>
{
    public async Task<Result<string>> Handle(GetRecordingDownloadUrlQuery request, CancellationToken cancellationToken)
    {
        if (!currentUserService.IsAuthenticated || currentUserService.ExternalId is null)
            return Result<string>.Failure("User is not authenticated.");

        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.ExternalId == currentUserService.ExternalId, cancellationToken);

        if (user is null)
            return Result<string>.Failure("User not found.");

        var recording = await dbContext.Recordings
            .FirstOrDefaultAsync(r => r.Id == request.RecordingId && r.UserId == user.Id, cancellationToken);

        if (recording is null)
            return Result<string>.Failure("Recording not found.");

        string url = await fileStorage.GetPresignedUrlAsync(recording.ObjectKey, 3600, cancellationToken);

        return Result<string>.Success(url);
    }
}
