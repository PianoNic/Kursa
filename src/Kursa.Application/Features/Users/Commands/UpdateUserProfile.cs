using Kursa.Application.Common.Interfaces;
using Kursa.Application.Common.Models;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace Kursa.Application.Features.Users.Commands;

public sealed record UpdateUserProfileCommand(
    string? DisplayName,
    string? AvatarUrl) : ICommand<Result<UserDto>>;

public sealed class UpdateUserProfileValidator : AbstractValidator<UpdateUserProfileCommand>
{
    public UpdateUserProfileValidator()
    {
        RuleFor(x => x.DisplayName)
            .MaximumLength(256)
            .When(x => x.DisplayName is not null);

        RuleFor(x => x.AvatarUrl)
            .MaximumLength(1024)
            .When(x => x.AvatarUrl is not null);
    }
}

public sealed class UpdateUserProfileHandler(
    ICurrentUserService currentUserService,
    IUserSyncService userSyncService) : ICommandHandler<UpdateUserProfileCommand, Result<UserDto>>
{
    public async ValueTask<Result<UserDto>> Handle(UpdateUserProfileCommand request, CancellationToken cancellationToken)
    {
        if (currentUserService.ExternalId is null || currentUserService.Email is null)
        {
            return Result<UserDto>.Failure("User is not authenticated.");
        }

        var user = await userSyncService.SyncUserAsync(
            currentUserService.ExternalId,
            currentUserService.Email,
            currentUserService.DisplayName ?? currentUserService.Email,
            cancellationToken);

        if (request.DisplayName is not null)
            user.DisplayName = request.DisplayName;

        if (request.AvatarUrl is not null)
            user.AvatarUrl = request.AvatarUrl;

        return Result<UserDto>.Success(new UserDto(
            user.Id,
            user.Email,
            user.DisplayName,
            user.AvatarUrl,
            user.Role,
            user.OnboardingCompleted,
            user.MoodleUrl,
            user.MoodleToken is not null,
            user.CreatedAt));
    }
}
