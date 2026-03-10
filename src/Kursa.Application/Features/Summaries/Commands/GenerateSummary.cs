using Kursa.Application.Common.Interfaces;
using Kursa.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Kursa.Application.Features.Summaries.Commands;

public sealed record GenerateSummaryCommand(Guid ContentId) : IRequest<Result<string>>;

public sealed class GenerateSummaryHandler(
    ICurrentUserService currentUserService,
    IAppDbContext dbContext,
    ISummaryService summaryService,
    ILogger<GenerateSummaryHandler> logger) : IRequestHandler<GenerateSummaryCommand, Result<string>>
{
    public async Task<Result<string>> Handle(GenerateSummaryCommand request, CancellationToken cancellationToken)
    {
        if (!currentUserService.IsAuthenticated || currentUserService.ExternalId is null)
            return Result<string>.Failure("User is not authenticated.");

        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.ExternalId == currentUserService.ExternalId, cancellationToken);

        if (user is null)
            return Result<string>.Failure("User not found.");

        var pinnedContent = await dbContext.PinnedContents
            .FirstOrDefaultAsync(p => p.ContentId == request.ContentId && p.UserId == user.Id, cancellationToken);

        if (pinnedContent is null)
            return Result<string>.Failure("Content must be pinned before generating a summary.");

        try
        {
            string summary = await summaryService.GenerateSummaryAsync(request.ContentId, user.Id, cancellationToken);

            if (string.IsNullOrEmpty(summary))
                return Result<string>.Failure("Could not generate summary — no content text available.");

            return Result<string>.Success(summary);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to generate summary for content {ContentId}", request.ContentId);
            return Result<string>.Failure("Failed to generate summary. Please try again later.");
        }
    }
}
