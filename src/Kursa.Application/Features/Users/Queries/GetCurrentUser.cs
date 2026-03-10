using Kursa.Application.Common.Interfaces;
using Kursa.Application.Common.Models;
using Kursa.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Kursa.Application.Features.Users.Queries;

public sealed record GetCurrentUserQuery : IRequest<Result<UserDto>>;

public sealed class GetCurrentUserHandler(
    ICurrentUserService currentUserService,
    IUserSyncService userSyncService) : IRequestHandler<GetCurrentUserQuery, Result<UserDto>>
{
    public async Task<Result<UserDto>> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        if (currentUserService.ExternalId is null || currentUserService.Email is null)
        {
            return Result<UserDto>.Failure("User is not authenticated.");
        }

        User user = await userSyncService.SyncUserAsync(
            currentUserService.ExternalId,
            currentUserService.Email,
            currentUserService.DisplayName ?? currentUserService.Email,
            cancellationToken);

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
