using Kursa.Application.Common.Interfaces;
using Kursa.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Kursa.Application.Features.Chat.Queries;

public sealed record GetChatThreadsQuery : IRequest<Result<IReadOnlyList<ChatThreadDto>>>;

public sealed class GetChatThreadsHandler(
    ICurrentUserService currentUserService,
    IAppDbContext dbContext) : IRequestHandler<GetChatThreadsQuery, Result<IReadOnlyList<ChatThreadDto>>>
{
    public async Task<Result<IReadOnlyList<ChatThreadDto>>> Handle(GetChatThreadsQuery request, CancellationToken cancellationToken)
    {
        if (!currentUserService.IsAuthenticated || currentUserService.ExternalId is null)
            return Result<IReadOnlyList<ChatThreadDto>>.Failure("User is not authenticated.");

        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.ExternalId == currentUserService.ExternalId, cancellationToken);

        if (user is null)
            return Result<IReadOnlyList<ChatThreadDto>>.Failure("User not found.");

        List<ChatThreadDto> threads = await dbContext.ChatThreads
            .Where(t => t.UserId == user.Id)
            .OrderByDescending(t => t.UpdatedAt)
            .Select(t => new ChatThreadDto(t.Id, t.Title, t.CreatedAt, t.UpdatedAt))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<ChatThreadDto>>.Success(threads);
    }
}
