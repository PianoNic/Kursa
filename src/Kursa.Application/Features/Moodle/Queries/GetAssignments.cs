using Kursa.Application.Common.Interfaces;
using Kursa.Application.Common.Models;
using Kursa.Application.Features.Moodle.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Kursa.Application.Features.Moodle.Queries;

public sealed record GetAssignmentsQuery(int? CourseId = null) : IRequest<Result<IReadOnlyList<AssignmentViewDto>>>;

public sealed class GetAssignmentsHandler(
    ICurrentUserService currentUserService,
    IAppDbContext dbContext,
    IMoodleService moodleService) : IRequestHandler<GetAssignmentsQuery, Result<IReadOnlyList<AssignmentViewDto>>>
{
    public async Task<Result<IReadOnlyList<AssignmentViewDto>>> Handle(
        GetAssignmentsQuery request, CancellationToken cancellationToken)
    {
        if (!currentUserService.IsAuthenticated || currentUserService.ExternalId is null)
            return Result<IReadOnlyList<AssignmentViewDto>>.Failure("User is not authenticated.");

        var user = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.ExternalId == currentUserService.ExternalId, cancellationToken);

        if (user is null)
            return Result<IReadOnlyList<AssignmentViewDto>>.Failure("User not found.");

        if (string.IsNullOrEmpty(user.MoodleToken) || string.IsNullOrEmpty(user.MoodleUrl))
            return Result<IReadOnlyList<AssignmentViewDto>>.Failure("Moodle account is not linked.");

        IReadOnlyList<int>? courseIds = request.CourseId.HasValue
            ? [request.CourseId.Value]
            : null;

        MoodleAssignmentsResponseDto response = await moodleService.GetAssignmentsAsync(
            user.MoodleUrl, user.MoodleToken, courseIds, cancellationToken);

        DateTime utcNow = DateTime.UtcNow;

        var assignments = response.Courses
            .SelectMany(course => course.Assignments.Select(a => new AssignmentViewDto
            {
                Id = a.Id,
                CourseId = course.Id,
                CourseName = course.FullName,
                CourseShortName = course.ShortName,
                Name = a.Name,
                Description = a.Intro,
                DueDate = a.DueDate > 0 ? DateTimeOffset.FromUnixTimeSeconds(a.DueDate).UtcDateTime : null,
                OpenDate = a.AllowSubmissionsFromDate > 0 ? DateTimeOffset.FromUnixTimeSeconds(a.AllowSubmissionsFromDate).UtcDateTime : null,
                CutoffDate = a.CutoffDate > 0 ? DateTimeOffset.FromUnixTimeSeconds(a.CutoffDate).UtcDateTime : null,
                IsOverdue = a.DueDate > 0 && DateTimeOffset.FromUnixTimeSeconds(a.DueDate).UtcDateTime < utcNow,
                IsSubmittable = a.NoSubmissions == 0,
            }))
            .OrderBy(a => a.DueDate ?? DateTime.MaxValue)
            .ToList();

        return Result<IReadOnlyList<AssignmentViewDto>>.Success(assignments);
    }
}
