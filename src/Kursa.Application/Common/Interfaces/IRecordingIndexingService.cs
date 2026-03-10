namespace Kursa.Application.Common.Interfaces;

public interface IRecordingIndexingService
{
    Task IndexRecordingAsync(Guid recordingId, CancellationToken cancellationToken = default);

    Task RemoveRecordingIndexAsync(Guid recordingId, CancellationToken cancellationToken = default);
}
