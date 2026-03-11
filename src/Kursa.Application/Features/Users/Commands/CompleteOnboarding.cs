using Kursa.Application.Common.Interfaces;
using Kursa.Application.Common.Models;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace Kursa.Application.Features.Users.Commands;

public sealed record CompleteOnboardingCommand : ICommand<Result>;

public sealed class CompleteOnboardingHandler(
    ICurrentUserService currentUserService,
    IAppDbContext dbContext) : ICommandHandler<CompleteOnboardingCommand, Result>
{
    public async ValueTask<Result> Handle(CompleteOnboardingCommand request, CancellationToken cancellationToken)
    {
        if (!currentUserService.IsAuthenticated || currentUserService.ExternalId is null)
            return Result.Failure("User is not authenticated.");

        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.ExternalId == currentUserService.ExternalId, cancellationToken);

        if (user is null)
            return Result.Failure("User not found.");

        user.OnboardingCompleted = true;
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
