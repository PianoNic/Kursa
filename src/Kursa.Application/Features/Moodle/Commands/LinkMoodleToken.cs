using Kursa.Application.Common.Interfaces;
using Kursa.Application.Common.Models;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Kursa.Application.Features.Moodle.Commands;

public sealed record LinkMoodleTokenCommand(string Token) : IRequest<Result>;

public sealed class LinkMoodleTokenValidator : AbstractValidator<LinkMoodleTokenCommand>
{
    public LinkMoodleTokenValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty()
            .MaximumLength(512);
    }
}

public sealed class LinkMoodleTokenHandler(
    ICurrentUserService currentUserService,
    IAppDbContext dbContext) : IRequestHandler<LinkMoodleTokenCommand, Result>
{
    public async Task<Result> Handle(LinkMoodleTokenCommand request, CancellationToken cancellationToken)
    {
        if (currentUserService.ExternalId is null)
        {
            return Result.Failure("User is not authenticated.");
        }

        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.ExternalId == currentUserService.ExternalId, cancellationToken);

        if (user is null)
        {
            return Result.Failure("User not found.");
        }

        user.MoodleToken = request.Token;

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
