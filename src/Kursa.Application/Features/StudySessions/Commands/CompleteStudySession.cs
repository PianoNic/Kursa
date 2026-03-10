using FluentValidation;
using Kursa.Application.Common.Interfaces;
using Kursa.Application.Common.Models;
using Kursa.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Kursa.Application.Features.StudySessions.Commands;

public sealed record CompleteStudySessionCommand(
    Guid SessionId,
    int CompletedPomodoros,
    int TotalDurationSeconds,
    int CardsReviewed,
    int QuizQuestionsAnswered,
    int QuizCorrectAnswers) : IRequest<Result<StudySessionDto>>;

public sealed class CompleteStudySessionValidator : AbstractValidator<CompleteStudySessionCommand>
{
    public CompleteStudySessionValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
        RuleFor(x => x.CompletedPomodoros).GreaterThanOrEqualTo(0);
        RuleFor(x => x.TotalDurationSeconds).GreaterThanOrEqualTo(0);
    }
}

public sealed class CompleteStudySessionHandler(
    ICurrentUserService currentUserService,
    IAppDbContext dbContext) : IRequestHandler<CompleteStudySessionCommand, Result<StudySessionDto>>
{
    public async Task<Result<StudySessionDto>> Handle(CompleteStudySessionCommand request, CancellationToken cancellationToken)
    {
        if (!currentUserService.IsAuthenticated || currentUserService.ExternalId is null)
            return Result<StudySessionDto>.Failure("User is not authenticated.");

        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.ExternalId == currentUserService.ExternalId, cancellationToken);

        if (user is null)
            return Result<StudySessionDto>.Failure("User not found.");

        var session = await dbContext.StudySessions
            .FirstOrDefaultAsync(s => s.Id == request.SessionId && s.UserId == user.Id, cancellationToken);

        if (session is null)
            return Result<StudySessionDto>.Failure("Study session not found.");

        if (session.Status != StudySessionStatus.Active)
            return Result<StudySessionDto>.Failure("Session is already completed.");

        session.Status = StudySessionStatus.Completed;
        session.CompletedPomodoros = request.CompletedPomodoros;
        session.TotalDurationSeconds = request.TotalDurationSeconds;
        session.CardsReviewed = request.CardsReviewed;
        session.QuizQuestionsAnswered = request.QuizQuestionsAnswered;
        session.QuizCorrectAnswers = request.QuizCorrectAnswers;
        session.CompletedAt = DateTime.UtcNow;

        // Generate summary
        var parts = new List<string>();
        if (request.CompletedPomodoros > 0)
            parts.Add($"{request.CompletedPomodoros} pomodoro{(request.CompletedPomodoros == 1 ? "" : "s")}");
        if (request.CardsReviewed > 0)
            parts.Add($"{request.CardsReviewed} card{(request.CardsReviewed == 1 ? "" : "s")} reviewed");
        if (request.QuizQuestionsAnswered > 0)
            parts.Add($"{request.QuizCorrectAnswers}/{request.QuizQuestionsAnswered} quiz questions correct");

        session.Summary = parts.Count > 0
            ? string.Join(", ", parts)
            : "Session completed";

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<StudySessionDto>.Success(new StudySessionDto(
            session.Id, session.Title, session.Status, session.WorkMinutes, session.BreakMinutes,
            session.CompletedPomodoros, session.TotalDurationSeconds,
            session.CardsReviewed, session.QuizQuestionsAnswered, session.QuizCorrectAnswers,
            session.Summary, session.CreatedAt, session.CompletedAt));
    }
}
