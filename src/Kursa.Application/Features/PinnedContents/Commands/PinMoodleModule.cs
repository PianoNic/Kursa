using FluentValidation;
using Kursa.Application.Common.Interfaces;
using Kursa.Application.Common.Models;
using Kursa.Domain.Entities;
using Kursa.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Kursa.Application.Features.PinnedContents.Commands;

/// <summary>
/// Pins a Moodle module/resource by lazily creating the local Course → Module → Content
/// hierarchy and scheduling it for RAG indexing. Returns immediately; indexing runs in
/// the background so the response is fast even for large content.
/// </summary>
public sealed record PinMoodleModuleCommand(
    int MoodleCourseId,
    string CourseName,
    string CourseShortName,
    int MoodleModuleId,
    string ModuleName,
    string ModType,
    string? Description,
    string? Url,
    string? FileUrl
) : IRequest<Result<PinMoodleModuleResponseDto>>;

public sealed record PinMoodleModuleResponseDto(Guid ContentId, bool AlreadyIndexed);

public sealed class PinMoodleModuleValidator : AbstractValidator<PinMoodleModuleCommand>
{
    public PinMoodleModuleValidator()
    {
        RuleFor(x => x.MoodleCourseId).GreaterThan(0);
        RuleFor(x => x.CourseName).NotEmpty().MaximumLength(500);
        RuleFor(x => x.MoodleModuleId).GreaterThan(0);
        RuleFor(x => x.ModuleName).NotEmpty().MaximumLength(500);
        RuleFor(x => x.ModType).NotEmpty().MaximumLength(100);
    }
}

public sealed class PinMoodleModuleHandler(
    ICurrentUserService currentUserService,
    IAppDbContext dbContext,
    IContentPipeline contentPipeline,
    ILogger<PinMoodleModuleHandler> logger) : IRequestHandler<PinMoodleModuleCommand, Result<PinMoodleModuleResponseDto>>
{
    public async Task<Result<PinMoodleModuleResponseDto>> Handle(
        PinMoodleModuleCommand request, CancellationToken cancellationToken)
    {
        if (!currentUserService.IsAuthenticated || currentUserService.ExternalId is null)
            return Result<PinMoodleModuleResponseDto>.Failure("User is not authenticated.");

        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.ExternalId == currentUserService.ExternalId, cancellationToken);

        if (user is null)
            return Result<PinMoodleModuleResponseDto>.Failure("User not found.");

        // --- 1. Find or create Course ---
        Course? course = await dbContext.Courses
            .FirstOrDefaultAsync(c => c.MoodleCourseId == request.MoodleCourseId, cancellationToken);

        if (course is null)
        {
            course = new Course
            {
                Name = request.CourseName,
                ShortName = request.CourseShortName,
                MoodleCourseId = request.MoodleCourseId,
            };
            dbContext.Courses.Add(course);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        // --- 2. Find or create Module ---
        Module? module = await dbContext.Modules
            .FirstOrDefaultAsync(m => m.MoodleModuleId == request.MoodleModuleId && m.CourseId == course.Id, cancellationToken);

        if (module is null)
        {
            module = new Module
            {
                Name = request.ModuleName,
                MoodleModuleId = request.MoodleModuleId,
                CourseId = course.Id,
            };
            dbContext.Modules.Add(module);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        // --- 3. Find or create Content ---
        Content? content = await dbContext.Contents
            .FirstOrDefaultAsync(c => c.MoodleContentId == request.MoodleModuleId && c.ModuleId == module.Id, cancellationToken);

        if (content is null)
        {
            content = new Content
            {
                Title = request.ModuleName,
                Description = request.Description,
                Type = MapContentType(request.ModType, request.FileUrl),
                Url = request.FileUrl ?? request.Url,
                MoodleContentId = request.MoodleModuleId,
                ModuleId = module.Id,
            };
            dbContext.Contents.Add(content);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        // --- 4. Pin the content ---
        PinnedContent? pinned = await dbContext.PinnedContents
            .FirstOrDefaultAsync(p => p.ContentId == content.Id && p.UserId == user.Id, cancellationToken);

        bool alreadyIndexed = false;
        if (pinned is null)
        {
            pinned = new PinnedContent
            {
                UserId = user.Id,
                ContentId = content.Id,
            };
            dbContext.PinnedContents.Add(pinned);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        else
        {
            alreadyIndexed = pinned.IsIndexed;
        }

        // --- 5. Trigger indexing in background ---
        if (!alreadyIndexed)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await contentPipeline.IndexContentAsync(content.Id, user.Id, CancellationToken.None);
                    logger.LogInformation("Background indexing complete for content {ContentId} ({Title})", content.Id, content.Title);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Background indexing failed for content {ContentId}", content.Id);
                }
            });
        }

        return Result<PinMoodleModuleResponseDto>.Success(new PinMoodleModuleResponseDto(content.Id, alreadyIndexed));
    }

    private static ContentType MapContentType(string modType, string? fileUrl) => modType switch
    {
        "quiz" => ContentType.Quiz,
        "assign" => ContentType.Assignment,
        "url" => ContentType.Link,
        "page" => ContentType.Html,
        "resource" when fileUrl?.Contains(".pdf", StringComparison.OrdinalIgnoreCase) == true => ContentType.Pdf,
        _ => ContentType.Document,
    };
}
