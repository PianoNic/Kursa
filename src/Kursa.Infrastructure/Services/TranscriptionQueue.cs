using System.Threading.Channels;
using Kursa.Application.Common.Interfaces;

namespace Kursa.Infrastructure.Services;

public sealed class TranscriptionQueue : ITranscriptionQueue
{
    private readonly Channel<Guid> _channel = Channel.CreateUnbounded<Guid>(
        new UnboundedChannelOptions { SingleReader = true });

    public ValueTask EnqueueAsync(Guid recordingId, CancellationToken cancellationToken = default)
    {
        return _channel.Writer.WriteAsync(recordingId, cancellationToken);
    }

    public ValueTask<Guid> DequeueAsync(CancellationToken cancellationToken)
    {
        return _channel.Reader.ReadAsync(cancellationToken);
    }
}
