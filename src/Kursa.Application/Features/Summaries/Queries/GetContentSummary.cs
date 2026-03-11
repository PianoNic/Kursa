using Kursa.Application.Common.Interfaces;
using Kursa.Application.Common.Models;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace Kursa.Application.Features.Summaries.Queries;

public sealed record ContentSummaryDto(Guid Id, Guid ContentId, string ContentTitle, string Summary, int TokensUsed, DateTime GeneratedAt);

public sealed record GetContentSummaryQuery(Guid ContentId) : IQuery<Result<ContentSummaryDto>>;

public sealed class GetContentSummaryHandler(
    ICurrentUserService currentUserService,
    IAppDbContext dbContext) : IQueryHandler<GetContentSummaryQuery, Result<ContentSummaryDto>>
{
    public async ValueTask<Result<ContentSummaryDto>> Handle(GetContentSummaryQuery request, CancellationToken cancellationToken)
    {
        if (!currentUserService.IsAuthenticated || currentUserService.ExternalId is null)
            return Result<ContentSummaryDto>.Failure("User is not authenticated.");

        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.ExternalId == currentUserService.ExternalId, cancellationToken);

        if (user is null)
            return Result<ContentSummaryDto>.Failure("User not found.");

        var summary = await dbContext.ContentSummaries
            .Include(s => s.Content)
            .FirstOrDefaultAsync(s => s.ContentId == request.ContentId && s.UserId == user.Id, cancellationToken);

        if (summary is null)
            return Result<ContentSummaryDto>.Failure("No summary found for this content.");

        return Result<ContentSummaryDto>.Success(new ContentSummaryDto(
            summary.Id,
            summary.ContentId,
            summary.Content.Title,
            summary.Summary,
            summary.TokensUsed,
            summary.CreatedAt));
    }
}
