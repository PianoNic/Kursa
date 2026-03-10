using FluentValidation;
using Kursa.Application.Common.Interfaces;
using Kursa.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Kursa.Application.Features.PinnedContents.Commands;

public sealed record UnpinContentCommand(Guid ContentId) : IRequest<Result>;

public sealed class UnpinContentValidator : AbstractValidator<UnpinContentCommand>
{
    public UnpinContentValidator()
    {
        RuleFor(x => x.ContentId).NotEmpty().WithMessage("Content ID is required.");
    }
}

public sealed class UnpinContentHandler(
    ICurrentUserService currentUserService,
    IAppDbContext dbContext) : IRequestHandler<UnpinContentCommand, Result>
{
    public async Task<Result> Handle(UnpinContentCommand request, CancellationToken cancellationToken)
    {
        if (!currentUserService.IsAuthenticated || currentUserService.ExternalId is null)
            return Result.Failure("User is not authenticated.");

        var user = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.ExternalId == currentUserService.ExternalId, cancellationToken);

        if (user is null)
            return Result.Failure("User not found.");

        var pinned = await dbContext.PinnedContents
            .FirstOrDefaultAsync(p => p.UserId == user.Id && p.ContentId == request.ContentId, cancellationToken);

        if (pinned is null)
            return Result.Success();

        dbContext.PinnedContents.Remove(pinned);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
