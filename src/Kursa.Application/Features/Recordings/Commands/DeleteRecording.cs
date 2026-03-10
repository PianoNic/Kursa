using Kursa.Application.Common.Interfaces;
using Kursa.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Kursa.Application.Features.Recordings.Commands;

public sealed record DeleteRecordingCommand(Guid RecordingId) : IRequest<Result<bool>>;

public sealed class DeleteRecordingHandler(
    ICurrentUserService currentUserService,
    IAppDbContext dbContext,
    IFileStorageService fileStorage) : IRequestHandler<DeleteRecordingCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(DeleteRecordingCommand request, CancellationToken cancellationToken)
    {
        if (!currentUserService.IsAuthenticated || currentUserService.ExternalId is null)
            return Result<bool>.Failure("User is not authenticated.");

        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.ExternalId == currentUserService.ExternalId, cancellationToken);

        if (user is null)
            return Result<bool>.Failure("User not found.");

        var recording = await dbContext.Recordings
            .FirstOrDefaultAsync(r => r.Id == request.RecordingId && r.UserId == user.Id, cancellationToken);

        if (recording is null)
            return Result<bool>.Failure("Recording not found.");

        await fileStorage.DeleteAsync(recording.ObjectKey, cancellationToken);

        dbContext.Recordings.Remove(recording);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}
