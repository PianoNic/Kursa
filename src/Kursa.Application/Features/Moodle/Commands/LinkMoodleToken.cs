using Kursa.Application.Common.Interfaces;
using Kursa.Application.Common.Models;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Kursa.Application.Features.Moodle.Commands;

public sealed record LinkMoodleTokenCommand(string Username, string Password) : IRequest<Result>;

public sealed class LinkMoodleTokenValidator : AbstractValidator<LinkMoodleTokenCommand>
{
    public LinkMoodleTokenValidator()
    {
        RuleFor(x => x.Username).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Password).NotEmpty().MaximumLength(512);
    }
}

public sealed class LinkMoodleTokenHandler(
    ICurrentUserService currentUserService,
    IAppDbContext dbContext,
    IMoodleService moodleService) : IRequestHandler<LinkMoodleTokenCommand, Result>
{
    public async Task<Result> Handle(LinkMoodleTokenCommand request, CancellationToken cancellationToken)
    {
        if (currentUserService.ExternalId is null)
        {
            return Result.Failure("User is not authenticated.");
        }

        var token = await moodleService.GetTokenAsync(request.Username, request.Password, cancellationToken);
        if (token is null)
        {
            return Result.Failure("Invalid Moodle credentials.");
        }

        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.ExternalId == currentUserService.ExternalId, cancellationToken);

        if (user is null)
        {
            return Result.Failure("User not found.");
        }

        user.MoodleToken = token;

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
