using Kursa.Application.Common.Interfaces;
using Kursa.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Kursa.Application.Features.Moodle.Queries;

public sealed record MoodleConnectionStatusDto(
    bool IsConnected,
    string? MoodleUrl);

public sealed record GetMoodleConnectionStatusQuery : IRequest<Result<MoodleConnectionStatusDto>>;

public sealed class GetMoodleConnectionStatusHandler(
    ICurrentUserService currentUserService,
    IAppDbContext dbContext) : IRequestHandler<GetMoodleConnectionStatusQuery, Result<MoodleConnectionStatusDto>>
{
    public async Task<Result<MoodleConnectionStatusDto>> Handle(
        GetMoodleConnectionStatusQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUserService.ExternalId is null)
        {
            return Result<MoodleConnectionStatusDto>.Failure("User is not authenticated.");
        }

        var user = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.ExternalId == currentUserService.ExternalId, cancellationToken);

        if (user is null)
        {
            return Result<MoodleConnectionStatusDto>.Failure("User not found.");
        }

        return Result<MoodleConnectionStatusDto>.Success(new MoodleConnectionStatusDto(
            user.MoodleToken is not null,
            user.MoodleUrl));
    }
}
