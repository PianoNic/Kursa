using Kursa.Application.Common.Interfaces;
using Kursa.Application.Common.Models;
using Kursa.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Kursa.Application.Features.Quizzes.Queries;

public sealed record GetQuizResultsQuery(Guid QuizId) : IRequest<Result<IReadOnlyList<QuizAttemptDto>>>;

public sealed class GetQuizResultsHandler(
    ICurrentUserService currentUserService,
    IAppDbContext dbContext) : IRequestHandler<GetQuizResultsQuery, Result<IReadOnlyList<QuizAttemptDto>>>
{
    public async Task<Result<IReadOnlyList<QuizAttemptDto>>> Handle(GetQuizResultsQuery request, CancellationToken cancellationToken)
    {
        if (!currentUserService.IsAuthenticated || currentUserService.ExternalId is null)
            return Result<IReadOnlyList<QuizAttemptDto>>.Failure("User is not authenticated.");

        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.ExternalId == currentUserService.ExternalId, cancellationToken);

        if (user is null)
            return Result<IReadOnlyList<QuizAttemptDto>>.Failure("User not found.");

        // Verify quiz belongs to user
        bool quizExists = await dbContext.Quizzes
            .AnyAsync(q => q.Id == request.QuizId && q.UserId == user.Id, cancellationToken);

        if (!quizExists)
            return Result<IReadOnlyList<QuizAttemptDto>>.Failure("Quiz not found.");

        List<QuizAttemptDto> attempts = await dbContext.QuizAttempts
            .Where(a => a.QuizId == request.QuizId && a.UserId == user.Id)
            .OrderByDescending(a => a.CompletedAt)
            .Select(a => new QuizAttemptDto(
                a.Id,
                a.QuizId,
                a.Score,
                a.TotalQuestions,
                a.DurationSeconds,
                a.CompletedAt ?? a.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<QuizAttemptDto>>.Success(attempts);
    }
}
