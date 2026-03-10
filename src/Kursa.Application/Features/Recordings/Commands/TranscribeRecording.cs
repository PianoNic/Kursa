using Kursa.Application.Common.Interfaces;
using Kursa.Application.Common.Models;
using Kursa.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Kursa.Application.Features.Recordings.Commands;

public sealed record TranscribeRecordingCommand(Guid RecordingId) : IRequest<Result<bool>>;

public sealed class TranscribeRecordingHandler(
    ICurrentUserService currentUserService,
    IAppDbContext dbContext,
    ITranscriptionQueue transcriptionQueue) : IRequestHandler<TranscribeRecordingCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(TranscribeRecordingCommand request, CancellationToken cancellationToken)
    {
        if (!currentUserService.IsAuthenticated || currentUserService.ExternalId is null)
            return Result<bool>.Failure("User is not authenticated.");

        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.ExternalId == currentUserService.ExternalId, cancellationToken);

        if (user is null)
            return Result<bool>.Failure("User not found.");

        Recording? recording = await dbContext.Recordings
            .FirstOrDefaultAsync(r => r.Id == request.RecordingId && r.UserId == user.Id, cancellationToken);

        if (recording is null)
            return Result<bool>.Failure("Recording not found.");

        if (recording.Status == RecordingStatus.Transcribing)
            return Result<bool>.Failure("Recording is already being transcribed.");

        if (recording.Status == RecordingStatus.Completed || recording.Status == RecordingStatus.Transcribed)
            return Result<bool>.Failure("Recording has already been transcribed.");

        recording.Status = RecordingStatus.Transcribing;
        recording.ErrorMessage = null;
        await dbContext.SaveChangesAsync(cancellationToken);

        await transcriptionQueue.EnqueueAsync(recording.Id, cancellationToken);

        return Result<bool>.Success(true);
    }
}
