using FluentValidation;
using Kursa.Application.Common.Interfaces;
using Kursa.Application.Common.Models;
using Kursa.Domain.Entities;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace Kursa.Application.Features.PinnedContents.Commands;

public sealed record PinContentCommand(Guid ContentId, string? Notes = null) : ICommand<Result<Guid>>;

public sealed class PinContentValidator : AbstractValidator<PinContentCommand>
{
    public PinContentValidator()
    {
        RuleFor(x => x.ContentId).NotEmpty().WithMessage("Content ID is required.");
        RuleFor(x => x.Notes).MaximumLength(2000).WithMessage("Notes cannot exceed 2000 characters.");
    }
}

public sealed class PinContentHandler(
    ICurrentUserService currentUserService,
    IAppDbContext dbContext) : ICommandHandler<PinContentCommand, Result<Guid>>
{
    public async ValueTask<Result<Guid>> Handle(PinContentCommand request, CancellationToken cancellationToken)
    {
        if (!currentUserService.IsAuthenticated || currentUserService.ExternalId is null)
            return Result<Guid>.Failure("User is not authenticated.");

        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.ExternalId == currentUserService.ExternalId, cancellationToken);

        if (user is null)
            return Result<Guid>.Failure("User not found.");

        var content = await dbContext.Contents
            .FirstOrDefaultAsync(c => c.Id == request.ContentId, cancellationToken);

        if (content is null)
            return Result<Guid>.Failure("Content not found.");

        var existing = await dbContext.PinnedContents
            .FirstOrDefaultAsync(p => p.UserId == user.Id && p.ContentId == request.ContentId, cancellationToken);

        if (existing is not null)
            return Result<Guid>.Success(existing.Id);

        var pinned = new PinnedContent
        {
            UserId = user.Id,
            ContentId = request.ContentId,
            Notes = request.Notes,
        };

        dbContext.PinnedContents.Add(pinned);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(pinned.Id);
    }
}
