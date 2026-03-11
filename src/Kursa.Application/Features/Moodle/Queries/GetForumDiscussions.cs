using Kursa.Application.Common.Interfaces;
using Kursa.Application.Common.Models;
using Kursa.Application.Features.Moodle.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Kursa.Application.Features.Moodle.Queries;

public sealed record GetForumDiscussionsQuery(int ForumId) : IRequest<Result<IReadOnlyList<DiscussionViewDto>>>;

public sealed class GetForumDiscussionsHandler(
    ICurrentUserService currentUserService,
    IAppDbContext dbContext,
    IMoodleService moodleService) : IRequestHandler<GetForumDiscussionsQuery, Result<IReadOnlyList<DiscussionViewDto>>>
{
    public async Task<Result<IReadOnlyList<DiscussionViewDto>>> Handle(
        GetForumDiscussionsQuery request, CancellationToken cancellationToken)
    {
        if (!currentUserService.IsAuthenticated || currentUserService.ExternalId is null)
            return Result<IReadOnlyList<DiscussionViewDto>>.Failure("User is not authenticated.");

        var user = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.ExternalId == currentUserService.ExternalId, cancellationToken);

        if (user is null)
            return Result<IReadOnlyList<DiscussionViewDto>>.Failure("User not found.");

        if (string.IsNullOrEmpty(user.MoodleToken))
            return Result<IReadOnlyList<DiscussionViewDto>>.Failure("Moodle account is not linked.");

        MoodleForumDiscussionsResponseDto response = await moodleService.GetForumDiscussionsAsync(
            user.MoodleToken, request.ForumId, cancellationToken);

        var result = response.Discussions
            .Select(d => new DiscussionViewDto
            {
                Id = d.Id,
                Title = d.Name,
                Message = d.Message,
                Author = d.UserFullName,
                AuthorAvatar = d.UserPictureUrl,
                CreatedAt = DateTimeOffset.FromUnixTimeSeconds(d.Created).UtcDateTime,
                ModifiedAt = DateTimeOffset.FromUnixTimeSeconds(d.Modified).UtcDateTime,
                ReplyCount = d.NumReplies,
                IsPinned = d.Pinned,
            })
            .OrderByDescending(d => d.IsPinned)
            .ThenByDescending(d => d.ModifiedAt)
            .ToList();

        return Result<IReadOnlyList<DiscussionViewDto>>.Success(result);
    }
}
