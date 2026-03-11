using Kursa.Application.Common.Interfaces;
using Kursa.Application.Common.Models;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace Kursa.Application.Features.Quizzes.Queries;

public sealed record GetQuizzesQuery : IQuery<Result<IReadOnlyList<QuizDto>>>;

public sealed class GetQuizzesHandler(
    ICurrentUserService currentUserService,
    IAppDbContext dbContext) : IQueryHandler<GetQuizzesQuery, Result<IReadOnlyList<QuizDto>>>
{
    public async ValueTask<Result<IReadOnlyList<QuizDto>>> Handle(GetQuizzesQuery request, CancellationToken cancellationToken)
    {
        if (!currentUserService.IsAuthenticated || currentUserService.ExternalId is null)
            return Result<IReadOnlyList<QuizDto>>.Failure("User is not authenticated.");

        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.ExternalId == currentUserService.ExternalId, cancellationToken);

        if (user is null)
            return Result<IReadOnlyList<QuizDto>>.Failure("User not found.");

        List<QuizDto> quizzes = await dbContext.Quizzes
            .Where(q => q.UserId == user.Id)
            .OrderByDescending(q => q.CreatedAt)
            .Select(q => new QuizDto(
                q.Id,
                q.Title,
                q.Topic,
                q.QuestionCount,
                q.DurationSeconds,
                q.Attempts.Count,
                q.Attempts.Any() ? q.Attempts.Max(a => a.Score) : null,
                q.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<QuizDto>>.Success(quizzes);
    }
}
