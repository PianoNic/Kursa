using Kursa.Application.Common.Interfaces;
using Kursa.Application.Common.Models;
using Kursa.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Kursa.Application.Features.Recordings.Commands;

public sealed record UploadRecordingCommand(
    string Title,
    string? Description,
    Guid? CourseId,
    string FileName,
    string ContentType,
    long FileSizeBytes,
    Stream FileStream) : IRequest<Result<RecordingDto>>;

public sealed class UploadRecordingHandler(
    ICurrentUserService currentUserService,
    IAppDbContext dbContext,
    IFileStorageService fileStorage,
    ITranscriptionQueue transcriptionQueue) : IRequestHandler<UploadRecordingCommand, Result<RecordingDto>>
{
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "audio/mpeg",
        "audio/mp3",
        "audio/wav",
        "audio/x-wav",
        "audio/ogg",
        "audio/flac",
        "audio/aac",
        "audio/mp4",
        "audio/x-m4a",
        "audio/webm",
    };

    private const long MaxFileSizeBytes = 500 * 1024 * 1024; // 500 MB

    public async Task<Result<RecordingDto>> Handle(UploadRecordingCommand request, CancellationToken cancellationToken)
    {
        if (!currentUserService.IsAuthenticated || currentUserService.ExternalId is null)
            return Result<RecordingDto>.Failure("User is not authenticated.");

        User? user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.ExternalId == currentUserService.ExternalId, cancellationToken);

        if (user is null)
            return Result<RecordingDto>.Failure("User not found.");

        if (!AllowedContentTypes.Contains(request.ContentType))
            return Result<RecordingDto>.Failure($"Unsupported audio format: {request.ContentType}. Supported: MP3, WAV, OGG, FLAC, AAC, M4A, WebM.");

        if (request.FileSizeBytes > MaxFileSizeBytes)
            return Result<RecordingDto>.Failure($"File too large. Maximum size is {MaxFileSizeBytes / (1024 * 1024)} MB.");

        if (request.CourseId.HasValue)
        {
            bool courseExists = await dbContext.Courses
                .AnyAsync(c => c.Id == request.CourseId.Value, cancellationToken);

            if (!courseExists)
                return Result<RecordingDto>.Failure("Course not found.");
        }

        string objectKey = $"recordings/{user.Id}/{Guid.NewGuid()}/{request.FileName}";

        await fileStorage.UploadAsync(objectKey, request.FileStream, request.ContentType, cancellationToken);

        var recording = new Recording
        {
            UserId = user.Id,
            Title = request.Title,
            Description = request.Description,
            CourseId = request.CourseId,
            FileName = request.FileName,
            ContentType = request.ContentType,
            FileSizeBytes = request.FileSizeBytes,
            ObjectKey = objectKey,
            Status = RecordingStatus.Uploaded,
        };

        dbContext.Recordings.Add(recording);
        await dbContext.SaveChangesAsync(cancellationToken);

        // Auto-enqueue transcription
        await transcriptionQueue.EnqueueAsync(recording.Id, cancellationToken);

        string? courseTitle = request.CourseId.HasValue
            ? await dbContext.Courses
                .Where(c => c.Id == request.CourseId.Value)
                .Select(c => c.Name)
                .FirstOrDefaultAsync(cancellationToken)
            : null;

        return Result<RecordingDto>.Success(new RecordingDto(
            recording.Id,
            recording.Title,
            recording.Description,
            recording.CourseId,
            courseTitle,
            recording.FileName,
            recording.ContentType,
            recording.FileSizeBytes,
            recording.DurationSeconds,
            recording.Status,
            false,
            recording.CreatedAt));
    }
}
