using System.Net.Http.Headers;
using System.Text.Json;
using Kursa.Application.Common.Interfaces;
using Kursa.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Kursa.Infrastructure.Services;

public sealed class WhisperTranscriptionService(
    IHttpClientFactory httpClientFactory,
    IOptions<AudioProcessingOptions> options,
    ILogger<WhisperTranscriptionService> logger) : ITranscriptionService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public async Task<TranscriptionResult> TranscribeAsync(
        Stream audioStream,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        try
        {
            HttpClient client = httpClientFactory.CreateClient("Whisper");
            client.BaseAddress = new Uri(options.Value.WhisperUrl);
            client.Timeout = TimeSpan.FromMinutes(30);

            using var content = new MultipartFormDataContent();
            var streamContent = new StreamContent(audioStream);
            streamContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            content.Add(streamContent, "file", "audio");

            HttpResponseMessage response = await client.PostAsync("/transcribe", content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                string errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                logger.LogError("Whisper transcription failed with status {Status}: {Body}",
                    response.StatusCode, errorBody);
                return new TranscriptionResult(false, null, null, null, $"Transcription service returned {response.StatusCode}");
            }

            string responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            WhisperResponse? result = JsonSerializer.Deserialize<WhisperResponse>(responseBody, JsonOptions);

            if (result is null)
                return new TranscriptionResult(false, null, null, null, "Invalid response from transcription service.");

            int? durationSeconds = result.Duration.HasValue ? (int)Math.Round(result.Duration.Value) : null;

            List<TranscriptionSegment>? segments = result.Segments?
                .Select(s => new TranscriptionSegment(s.Start, s.End, s.Text?.Trim() ?? ""))
                .Where(s => !string.IsNullOrWhiteSpace(s.Text))
                .ToList();

            return new TranscriptionResult(true, result.Text, durationSeconds, segments, null);
        }
        catch (TaskCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Whisper transcription failed");
            return new TranscriptionResult(false, null, null, null, $"Transcription failed: {ex.Message}");
        }
    }

    private sealed class WhisperResponse
    {
        public string? Text { get; init; }
        public double? Duration { get; init; }
        public List<WhisperSegment>? Segments { get; init; }
    }

    private sealed class WhisperSegment
    {
        public double Start { get; init; }
        public double End { get; init; }
        public string? Text { get; init; }
    }
}
