namespace Kursa.Application.Common.Interfaces;

public interface ITranscriptionQueue
{
    ValueTask EnqueueAsync(Guid recordingId, CancellationToken cancellationToken = default);
}
