using FluentValidation;
using Kursa.Application.Common.Interfaces;
using Kursa.Application.Common.Models;
using Kursa.Domain.Entities;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace Kursa.Application.Features.StudySessions.Commands;

public sealed record StartStudySessionCommand(
    string Title,
    int WorkMinutes = 25,
    int BreakMinutes = 5) : ICommand<Result<StudySessionDto>>;

public sealed class StartStudySessionValidator : AbstractValidator<StartStudySessionCommand>
{
    public StartStudySessionValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.WorkMinutes).InclusiveBetween(1, 120);
        RuleFor(x => x.BreakMinutes).InclusiveBetween(1, 60);
    }
}

public sealed class StartStudySessionHandler(
    ICurrentUserService currentUserService,
    IAppDbContext dbContext) : ICommandHandler<StartStudySessionCommand, Result<StudySessionDto>>
{
    public async ValueTask<Result<StudySessionDto>> Handle(StartStudySessionCommand request, CancellationToken cancellationToken)
    {
        if (!currentUserService.IsAuthenticated || currentUserService.ExternalId is null)
            return Result<StudySessionDto>.Failure("User is not authenticated.");

        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.ExternalId == currentUserService.ExternalId, cancellationToken);

        if (user is null)
            return Result<StudySessionDto>.Failure("User not found.");

        var session = new StudySession
        {
            UserId = user.Id,
            Title = request.Title,
            WorkMinutes = request.WorkMinutes,
            BreakMinutes = request.BreakMinutes,
        };

        dbContext.StudySessions.Add(session);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<StudySessionDto>.Success(MapToDto(session));
    }

    private static StudySessionDto MapToDto(StudySession s) => new(
        s.Id, s.Title, s.Status, s.WorkMinutes, s.BreakMinutes,
        s.CompletedPomodoros, s.TotalDurationSeconds,
        s.CardsReviewed, s.QuizQuestionsAnswered, s.QuizCorrectAnswers,
        s.Summary, s.CreatedAt, s.CompletedAt);
}
