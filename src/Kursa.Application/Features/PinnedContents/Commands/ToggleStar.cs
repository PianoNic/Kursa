using FluentValidation;
using Kursa.Application.Common.Interfaces;
using Kursa.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Kursa.Application.Features.PinnedContents.Commands;

public sealed record ToggleStarCommand(Guid ContentId) : IRequest<Result<bool>>;

public sealed class ToggleStarValidator : AbstractValidator<ToggleStarCommand>
{
    public ToggleStarValidator()
    {
        RuleFor(x => x.ContentId).NotEmpty().WithMessage("Content ID is required.");
    }
}

public sealed class ToggleStarHandler(
    ICurrentUserService currentUserService,
    IAppDbContext dbContext) : IRequestHandler<ToggleStarCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(ToggleStarCommand request, CancellationToken cancellationToken)
    {
        if (!currentUserService.IsAuthenticated || currentUserService.ExternalId is null)
            return Result<bool>.Failure("User is not authenticated.");

        var user = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.ExternalId == currentUserService.ExternalId, cancellationToken);

        if (user is null)
            return Result<bool>.Failure("User not found.");

        var pinned = await dbContext.PinnedContents
            .FirstOrDefaultAsync(p => p.UserId == user.Id && p.ContentId == request.ContentId, cancellationToken);

        if (pinned is null)
            return Result<bool>.Failure("Content is not pinned. Pin it first.");

        pinned.IsStarred = !pinned.IsStarred;
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(pinned.IsStarred);
    }
}
