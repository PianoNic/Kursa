using Kursa.Application.Common.Interfaces;
using Kursa.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Kursa.Application.Features.StudySessions.Queries;

public sealed record GetStudySessionsQuery : IRequest<Result<IReadOnlyList<StudySessionDto>>>;

public sealed class GetStudySessionsHandler(
    ICurrentUserService currentUserService,
    IAppDbContext dbContext) : IRequestHandler<GetStudySessionsQuery, Result<IReadOnlyList<StudySessionDto>>>
{
    public async Task<Result<IReadOnlyList<StudySessionDto>>> Handle(GetStudySessionsQuery request, CancellationToken cancellationToken)
    {
        if (!currentUserService.IsAuthenticated || currentUserService.ExternalId is null)
            return Result<IReadOnlyList<StudySessionDto>>.Failure("User is not authenticated.");

        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.ExternalId == currentUserService.ExternalId, cancellationToken);

        if (user is null)
            return Result<IReadOnlyList<StudySessionDto>>.Failure("User not found.");

        List<StudySessionDto> sessions = await dbContext.StudySessions
            .Where(s => s.UserId == user.Id)
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new StudySessionDto(
                s.Id, s.Title, s.Status, s.WorkMinutes, s.BreakMinutes,
                s.CompletedPomodoros, s.TotalDurationSeconds,
                s.CardsReviewed, s.QuizQuestionsAnswered, s.QuizCorrectAnswers,
                s.Summary, s.CreatedAt, s.CompletedAt))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<StudySessionDto>>.Success(sessions);
    }
}
