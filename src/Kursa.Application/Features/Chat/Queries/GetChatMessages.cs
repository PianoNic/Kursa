using Kursa.Application.Common.Interfaces;
using Kursa.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Kursa.Application.Features.Chat.Queries;

public sealed record GetChatMessagesQuery(Guid ThreadId) : IRequest<Result<IReadOnlyList<ChatMessageDto>>>;

public sealed class GetChatMessagesHandler(
    ICurrentUserService currentUserService,
    IAppDbContext dbContext) : IRequestHandler<GetChatMessagesQuery, Result<IReadOnlyList<ChatMessageDto>>>
{
    public async Task<Result<IReadOnlyList<ChatMessageDto>>> Handle(GetChatMessagesQuery request, CancellationToken cancellationToken)
    {
        if (!currentUserService.IsAuthenticated || currentUserService.ExternalId is null)
            return Result<IReadOnlyList<ChatMessageDto>>.Failure("User is not authenticated.");

        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.ExternalId == currentUserService.ExternalId, cancellationToken);

        if (user is null)
            return Result<IReadOnlyList<ChatMessageDto>>.Failure("User not found.");

        var thread = await dbContext.ChatThreads
            .FirstOrDefaultAsync(t => t.Id == request.ThreadId && t.UserId == user.Id, cancellationToken);

        if (thread is null)
            return Result<IReadOnlyList<ChatMessageDto>>.Failure("Thread not found.");

        List<ChatMessageDto> messages = await dbContext.ChatMessages
            .Where(m => m.ThreadId == request.ThreadId)
            .OrderBy(m => m.CreatedAt)
            .Select(m => new ChatMessageDto(m.Id, m.Role, m.Content, m.Citations, m.TokensUsed, m.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<ChatMessageDto>>.Success(messages);
    }
}
