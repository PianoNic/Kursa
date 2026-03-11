using System.Text.Json;
using Kursa.Application.Common.Interfaces;
using Kursa.Application.Common.Models;
using Kursa.Domain.Entities;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace Kursa.Application.Features.Quizzes.Queries;

public sealed record GetQuizDetailQuery(Guid QuizId) : IQuery<Result<QuizDetailDto>>;

public sealed class GetQuizDetailHandler(
    ICurrentUserService currentUserService,
    IAppDbContext dbContext) : IQueryHandler<GetQuizDetailQuery, Result<QuizDetailDto>>
{
    public async ValueTask<Result<QuizDetailDto>> Handle(GetQuizDetailQuery request, CancellationToken cancellationToken)
    {
        if (!currentUserService.IsAuthenticated || currentUserService.ExternalId is null)
            return Result<QuizDetailDto>.Failure("User is not authenticated.");

        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.ExternalId == currentUserService.ExternalId, cancellationToken);

        if (user is null)
            return Result<QuizDetailDto>.Failure("User not found.");

        var quiz = await dbContext.Quizzes
            .Include(q => q.Questions.OrderBy(qq => qq.OrderIndex))
            .FirstOrDefaultAsync(q => q.Id == request.QuizId && q.UserId == user.Id, cancellationToken);

        if (quiz is null)
            return Result<QuizDetailDto>.Failure("Quiz not found.");

        var questions = quiz.Questions.Select(q => new QuizQuestionDto(
            q.Id,
            q.QuestionText,
            q.Type,
            q.Options is not null ? JsonSerializer.Deserialize<List<string>>(q.Options) : null,
            q.OrderIndex)).ToList();

        return Result<QuizDetailDto>.Success(new QuizDetailDto(
            quiz.Id,
            quiz.Title,
            quiz.Topic,
            quiz.DurationSeconds,
            questions));
    }
}
