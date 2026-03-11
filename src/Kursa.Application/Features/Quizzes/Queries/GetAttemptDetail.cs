using Kursa.Application.Common.Interfaces;
using Kursa.Application.Common.Models;
using Kursa.Domain.Entities;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace Kursa.Application.Features.Quizzes.Queries;

public sealed record GetAttemptDetailQuery(Guid AttemptId) : IQuery<Result<QuizAttemptDetailDto>>;

public sealed class GetAttemptDetailHandler(
    ICurrentUserService currentUserService,
    IAppDbContext dbContext) : IQueryHandler<GetAttemptDetailQuery, Result<QuizAttemptDetailDto>>
{
    public async ValueTask<Result<QuizAttemptDetailDto>> Handle(GetAttemptDetailQuery request, CancellationToken cancellationToken)
    {
        if (!currentUserService.IsAuthenticated || currentUserService.ExternalId is null)
            return Result<QuizAttemptDetailDto>.Failure("User is not authenticated.");

        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.ExternalId == currentUserService.ExternalId, cancellationToken);

        if (user is null)
            return Result<QuizAttemptDetailDto>.Failure("User not found.");

        var attempt = await dbContext.QuizAttempts
            .Include(a => a.Answers)
                .ThenInclude(a => a.Question)
            .FirstOrDefaultAsync(a => a.Id == request.AttemptId && a.UserId == user.Id, cancellationToken);

        if (attempt is null)
            return Result<QuizAttemptDetailDto>.Failure("Attempt not found.");

        var answerDtos = attempt.Answers
            .Select(a => new QuizAnswerDto(
                a.QuestionId,
                a.Question.QuestionText,
                a.Question.Type,
                a.UserAnswer,
                a.Question.CorrectAnswer,
                a.Question.Explanation,
                a.IsCorrect))
            .ToList();

        return Result<QuizAttemptDetailDto>.Success(new QuizAttemptDetailDto(
            attempt.Id,
            attempt.QuizId,
            attempt.Score,
            attempt.TotalQuestions,
            attempt.DurationSeconds,
            attempt.CompletedAt ?? attempt.CreatedAt,
            answerDtos));
    }
}
