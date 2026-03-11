using Kursa.Application.Common.Interfaces;
using Kursa.Application.Common.Models;
using Kursa.Application.Features.Moodle.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Kursa.Application.Features.Moodle.Queries;

public sealed record GetForumsQuery(int CourseId) : IRequest<Result<IReadOnlyList<ForumViewDto>>>;

public sealed class GetForumsHandler(
    ICurrentUserService currentUserService,
    IAppDbContext dbContext,
    IMoodleService moodleService) : IRequestHandler<GetForumsQuery, Result<IReadOnlyList<ForumViewDto>>>
{
    public async Task<Result<IReadOnlyList<ForumViewDto>>> Handle(
        GetForumsQuery request, CancellationToken cancellationToken)
    {
        if (!currentUserService.IsAuthenticated || currentUserService.ExternalId is null)
            return Result<IReadOnlyList<ForumViewDto>>.Failure("User is not authenticated.");

        var user = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.ExternalId == currentUserService.ExternalId, cancellationToken);

        if (user is null)
            return Result<IReadOnlyList<ForumViewDto>>.Failure("User not found.");

        if (string.IsNullOrEmpty(user.MoodleToken))
            return Result<IReadOnlyList<ForumViewDto>>.Failure("Moodle account is not linked.");

        IReadOnlyList<MoodleForumDto> forums = await moodleService.GetForumsAsync(
            user.MoodleToken, request.CourseId, cancellationToken);

        var result = forums.Select(f => new ForumViewDto
        {
            Id = f.Id,
            CourseId = f.CourseId,
            Name = f.Name,
            Description = f.Intro,
            Type = f.Type,
            DiscussionCount = f.NumDiscussions,
            LastModified = f.TimeModified > 0
                ? DateTimeOffset.FromUnixTimeSeconds(f.TimeModified).UtcDateTime
                : null,
        }).ToList();

        return Result<IReadOnlyList<ForumViewDto>>.Success(result);
    }
}
