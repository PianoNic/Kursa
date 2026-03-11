using Kursa.Application.Common.Interfaces;
using Kursa.Application.Common.Models;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace Kursa.Application.Features.PinnedContents.Queries;

public sealed record GetPinnedContentsQuery : IQuery<Result<IReadOnlyList<PinnedContentDto>>>;

public sealed class GetPinnedContentsHandler(
    ICurrentUserService currentUserService,
    IAppDbContext dbContext) : IQueryHandler<GetPinnedContentsQuery, Result<IReadOnlyList<PinnedContentDto>>>
{
    public async ValueTask<Result<IReadOnlyList<PinnedContentDto>>> Handle(
        GetPinnedContentsQuery request, CancellationToken cancellationToken)
    {
        if (!currentUserService.IsAuthenticated || currentUserService.ExternalId is null)
            return Result<IReadOnlyList<PinnedContentDto>>.Failure("User is not authenticated.");

        var user = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.ExternalId == currentUserService.ExternalId, cancellationToken);

        if (user is null)
            return Result<IReadOnlyList<PinnedContentDto>>.Failure("User not found.");

        var pinnedContents = await dbContext.PinnedContents
            .AsNoTracking()
            .Include(p => p.Content)
            .Where(p => p.UserId == user.Id)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new PinnedContentDto(
                p.Id,
                p.ContentId,
                p.Content.Title,
                p.Content.Description,
                p.Content.Type.ToString(),
                p.Content.Url,
                p.IsStarred,
                p.IsIndexed,
                p.Notes,
                p.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<PinnedContentDto>>.Success(pinnedContents);
    }
}
