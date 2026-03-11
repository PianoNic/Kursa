using FluentValidation;
using Kursa.Application.Common.Interfaces;
using Kursa.Application.Common.Models;
using Kursa.Domain.Entities;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace Kursa.Application.Features.Quizzes.Commands;

public sealed record SubmitQuizAttemptCommand(
    Guid QuizId,
    IReadOnlyList<AnswerSubmission> Answers,
    int DurationSeconds) : ICommand<Result<QuizAttemptDetailDto>>;

public sealed record AnswerSubmission(Guid QuestionId, string Answer);

public sealed class SubmitQuizAttemptValidator : AbstractValidator<SubmitQuizAttemptCommand>
{
    public SubmitQuizAttemptValidator()
    {
        RuleFor(x => x.QuizId).NotEmpty();
        RuleFor(x => x.Answers).NotEmpty();
        RuleFor(x => x.DurationSeconds).GreaterThanOrEqualTo(0);
    }
}

public sealed class SubmitQuizAttemptHandler(
    ICurrentUserService currentUserService,
    IAppDbContext dbContext) : ICommandHandler<SubmitQuizAttemptCommand, Result<QuizAttemptDetailDto>>
{
    public async ValueTask<Result<QuizAttemptDetailDto>> Handle(SubmitQuizAttemptCommand request, CancellationToken cancellationToken)
    {
        if (!currentUserService.IsAuthenticated || currentUserService.ExternalId is null)
            return Result<QuizAttemptDetailDto>.Failure("User is not authenticated.");

        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.ExternalId == currentUserService.ExternalId, cancellationToken);

        if (user is null)
            return Result<QuizAttemptDetailDto>.Failure("User not found.");

        var quiz = await dbContext.Quizzes
            .Include(q => q.Questions)
            .FirstOrDefaultAsync(q => q.Id == request.QuizId && q.UserId == user.Id, cancellationToken);

        if (quiz is null)
            return Result<QuizAttemptDetailDto>.Failure("Quiz not found.");

        // Grade answers
        var questionMap = quiz.Questions.ToDictionary(q => q.Id);
        int score = 0;
        var answers = new List<QuizAnswer>();
        var answerDtos = new List<QuizAnswerDto>();

        foreach (AnswerSubmission submission in request.Answers)
        {
            if (!questionMap.TryGetValue(submission.QuestionId, out QuizQuestion? question))
                continue;

            bool isCorrect = string.Equals(
                submission.Answer.Trim(),
                question.CorrectAnswer.Trim(),
                StringComparison.OrdinalIgnoreCase);

            if (isCorrect) score++;

            var answer = new QuizAnswer
            {
                QuestionId = submission.QuestionId,
                UserAnswer = submission.Answer,
                IsCorrect = isCorrect,
            };
            answers.Add(answer);

            answerDtos.Add(new QuizAnswerDto(
                question.Id,
                question.QuestionText,
                question.Type,
                submission.Answer,
                question.CorrectAnswer,
                question.Explanation,
                isCorrect));
        }

        var attempt = new QuizAttempt
        {
            QuizId = quiz.Id,
            UserId = user.Id,
            Score = score,
            TotalQuestions = quiz.QuestionCount,
            DurationSeconds = request.DurationSeconds,
            CompletedAt = DateTime.UtcNow,
        };

        dbContext.QuizAttempts.Add(attempt);

        foreach (var answer in answers)
        {
            answer.AttemptId = attempt.Id;
            dbContext.QuizAnswers.Add(answer);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<QuizAttemptDetailDto>.Success(new QuizAttemptDetailDto(
            attempt.Id,
            attempt.QuizId,
            attempt.Score,
            attempt.TotalQuestions,
            attempt.DurationSeconds,
            attempt.CompletedAt ?? DateTime.UtcNow,
            answerDtos));
    }
}
