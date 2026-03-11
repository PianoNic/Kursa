using Kursa.Application.Common.Interfaces;
using Kursa.Application.Common.Models;
using Kursa.Application.Features.Moodle.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Kursa.Application.Features.Moodle.Queries;

public sealed record GetCalendarEventsQuery(DateTime WeekStart) : IRequest<Result<IReadOnlyList<CalendarEventViewDto>>>;

public sealed class GetCalendarEventsHandler(
    ICurrentUserService currentUserService,
    IAppDbContext dbContext,
    IMoodleService moodleService) : IRequestHandler<GetCalendarEventsQuery, Result<IReadOnlyList<CalendarEventViewDto>>>
{
    public async Task<Result<IReadOnlyList<CalendarEventViewDto>>> Handle(
        GetCalendarEventsQuery request, CancellationToken cancellationToken)
    {
        if (!currentUserService.IsAuthenticated || currentUserService.ExternalId is null)
            return Result<IReadOnlyList<CalendarEventViewDto>>.Failure("User is not authenticated.");

        var user = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.ExternalId == currentUserService.ExternalId, cancellationToken);

        if (user is null)
            return Result<IReadOnlyList<CalendarEventViewDto>>.Failure("User not found.");

        if (string.IsNullOrEmpty(user.MoodleToken))
            return Result<IReadOnlyList<CalendarEventViewDto>>.Failure("Moodle account is not linked.");

        DateTime weekEnd = request.WeekStart.AddDays(7);
        long timeStart = new DateTimeOffset(request.WeekStart, TimeSpan.Zero).ToUnixTimeSeconds();
        long timeEnd = new DateTimeOffset(weekEnd, TimeSpan.Zero).ToUnixTimeSeconds();

        MoodleCalendarEventsResponseDto response = await moodleService.GetCalendarEventsAsync(
            user.MoodleToken, timeStart, timeEnd, cancellationToken);

        var events = response.Events
            .Select(e =>
            {
                DateTime start = DateTimeOffset.FromUnixTimeSeconds(e.TimeStart).UtcDateTime;
                int durationMinutes = (int)(e.TimeDuration / 60);
                if (durationMinutes <= 0) durationMinutes = 60; // Default 1 hour

                return new CalendarEventViewDto
                {
                    Id = e.Id,
                    Title = e.Name,
                    Description = e.Description,
                    CourseId = e.CourseId,
                    StartTime = start,
                    EndTime = start.AddMinutes(durationMinutes),
                    DurationMinutes = durationMinutes,
                    EventType = e.EventType,
                    ModuleName = e.ModuleName,
                };
            })
            .OrderBy(e => e.StartTime)
            .ToList();

        return Result<IReadOnlyList<CalendarEventViewDto>>.Success(events);
    }
}
