using Kursa.Application.Common.Interfaces;
using Kursa.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Kursa.Application.Features.Moodle.Commands;

public sealed record UnlinkMoodleTokenCommand : IRequest<Result>;

public sealed class UnlinkMoodleTokenHandler(
    ICurrentUserService currentUserService,
    IAppDbContext dbContext) : IRequestHandler<UnlinkMoodleTokenCommand, Result>
{
    public async Task<Result> Handle(UnlinkMoodleTokenCommand request, CancellationToken cancellationToken)
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

        user.MoodleToken = null;
        user.MoodleUrl = null;

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
