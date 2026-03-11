using Kursa.Application.Common.Interfaces;
using Kursa.Application.Common.Models;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace Kursa.Application.Features.Moodle.Commands;

public sealed record UnlinkMoodleTokenCommand : ICommand<Result>;

public sealed class UnlinkMoodleTokenHandler(
    ICurrentUserService currentUserService,
    IAppDbContext dbContext) : ICommandHandler<UnlinkMoodleTokenCommand, Result>
{
    public async ValueTask<Result> Handle(UnlinkMoodleTokenCommand request, CancellationToken cancellationToken)
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
