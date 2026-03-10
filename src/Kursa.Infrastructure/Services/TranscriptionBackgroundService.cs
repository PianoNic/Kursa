using Kursa.Application.Common.Interfaces;
using Kursa.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Kursa.Infrastructure.Services;

public sealed class TranscriptionBackgroundService(
    IServiceScopeFactory scopeFactory,
    TranscriptionQueue queue,
    ILogger<TranscriptionBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Transcription background service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                Guid recordingId = await queue.DequeueAsync(stoppingToken);
                await ProcessTranscriptionAsync(recordingId, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing transcription job");
            }
        }
    }

    private async Task ProcessTranscriptionAsync(Guid recordingId, CancellationToken cancellationToken)
    {
        using IServiceScope scope = scopeFactory.CreateScope();
        IAppDbContext dbContext = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        IFileStorageService fileStorage = scope.ServiceProvider.GetRequiredService<IFileStorageService>();
        ITranscriptionService transcriptionService = scope.ServiceProvider.GetRequiredService<ITranscriptionService>();

        Recording? recording = await dbContext.Recordings
            .FirstOrDefaultAsync(r => r.Id == recordingId, cancellationToken);

        if (recording is null)
        {
            logger.LogWarning("Recording {RecordingId} not found for transcription", recordingId);
            return;
        }

        logger.LogInformation("Starting transcription for recording {RecordingId} ({Title})",
            recordingId, recording.Title);

        recording.Status = RecordingStatus.Transcribing;
        await dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            await using Stream audioStream = await fileStorage.DownloadAsync(recording.ObjectKey, cancellationToken);

            TranscriptionResult result = await transcriptionService.TranscribeAsync(
                audioStream, recording.ContentType, cancellationToken);

            if (result.Success)
            {
                recording.TranscriptText = result.Text;
                recording.DurationSeconds = result.DurationSeconds;
                recording.TranscribedAt = DateTime.UtcNow;
                recording.Status = RecordingStatus.Transcribed;
                recording.ErrorMessage = null;

                // Store segments if available
                if (result.Segments is { Count: > 0 })
                {
                    // Remove existing segments (in case of retry)
                    List<TranscriptSegment> existing = await dbContext.TranscriptSegments
                        .Where(s => s.RecordingId == recordingId)
                        .ToListAsync(cancellationToken);

                    if (existing.Count > 0)
                        dbContext.TranscriptSegments.RemoveRange(existing);

                    for (int i = 0; i < result.Segments.Count; i++)
                    {
                        TranscriptionSegment seg = result.Segments[i];
                        dbContext.TranscriptSegments.Add(new TranscriptSegment
                        {
                            RecordingId = recordingId,
                            OrderIndex = i,
                            StartSeconds = seg.StartSeconds,
                            EndSeconds = seg.EndSeconds,
                            Text = seg.Text,
                        });
                    }
                }

                logger.LogInformation("Transcription completed for recording {RecordingId}", recordingId);
            }
            else
            {
                recording.Status = RecordingStatus.Failed;
                recording.ErrorMessage = result.Error;

                logger.LogError("Transcription failed for recording {RecordingId}: {Error}",
                    recordingId, result.Error);
            }
        }
        catch (Exception ex)
        {
            recording.Status = RecordingStatus.Failed;
            recording.ErrorMessage = $"Transcription failed: {ex.Message}";

            logger.LogError(ex, "Transcription failed for recording {RecordingId}", recordingId);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
