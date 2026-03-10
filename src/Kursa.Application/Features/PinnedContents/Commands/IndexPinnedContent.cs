using Kursa.Application.Common.Interfaces;
using Kursa.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Kursa.Application.Features.PinnedContents.Commands;

public sealed record IndexPinnedContentCommand(Guid ContentId) : IRequest<Result>;

public sealed class IndexPinnedContentHandler(
    ICurrentUserService currentUserService,
    IAppDbContext dbContext,
    IContentPipeline contentPipeline,
    ILogger<IndexPinnedContentHandler> logger) : IRequestHandler<IndexPinnedContentCommand, Result>
{
    public async Task<Result> Handle(IndexPinnedContentCommand request, CancellationToken cancellationToken)
    {
        if (!currentUserService.IsAuthenticated || currentUserService.ExternalId is null)
            return Result.Failure("User is not authenticated.");

        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.ExternalId == currentUserService.ExternalId, cancellationToken);

        if (user is null)
            return Result.Failure("User not found.");

        var pinnedContent = await dbContext.PinnedContents
            .FirstOrDefaultAsync(p => p.ContentId == request.ContentId && p.UserId == user.Id, cancellationToken);

        if (pinnedContent is null)
            return Result.Failure("Content must be pinned before indexing.");

        if (pinnedContent.IsIndexed)
            return Result.Success();

        try
        {
            await contentPipeline.IndexContentAsync(request.ContentId, user.Id, cancellationToken);
            return Result.Success();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to index content {ContentId}", request.ContentId);
            return Result.Failure("Failed to index content. Please try again later.");
        }
    }
}
