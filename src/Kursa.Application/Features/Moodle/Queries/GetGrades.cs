using Kursa.Application.Common.Interfaces;
using Kursa.Application.Common.Models;
using Kursa.Application.Features.Moodle.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Kursa.Application.Features.Moodle.Queries;

public sealed record GetGradesQuery(int? CourseId = null) : IRequest<Result<IReadOnlyList<CourseGradeSummaryDto>>>;

public sealed class GetGradesHandler(
    ICurrentUserService currentUserService,
    IAppDbContext dbContext,
    IMoodleService moodleService) : IRequestHandler<GetGradesQuery, Result<IReadOnlyList<CourseGradeSummaryDto>>>
{
    public async Task<Result<IReadOnlyList<CourseGradeSummaryDto>>> Handle(
        GetGradesQuery request, CancellationToken cancellationToken)
    {
        if (!currentUserService.IsAuthenticated || currentUserService.ExternalId is null)
            return Result<IReadOnlyList<CourseGradeSummaryDto>>.Failure("User is not authenticated.");

        var user = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.ExternalId == currentUserService.ExternalId, cancellationToken);

        if (user is null)
            return Result<IReadOnlyList<CourseGradeSummaryDto>>.Failure("User not found.");

        if (string.IsNullOrEmpty(user.MoodleToken) || string.IsNullOrEmpty(user.MoodleUrl))
            return Result<IReadOnlyList<CourseGradeSummaryDto>>.Failure("Moodle account is not linked.");

        // If a specific course is requested, fetch just that one
        if (request.CourseId.HasValue)
        {
            CourseGradeSummaryDto summary = await GetCourseGradeSummaryAsync(
                user.MoodleUrl, user.MoodleToken, request.CourseId.Value, cancellationToken);
            return Result<IReadOnlyList<CourseGradeSummaryDto>>.Success([summary]);
        }

        // Otherwise fetch grades for all enrolled courses
        IReadOnlyList<MoodleCourseDto> courses = await moodleService.GetEnrolledCoursesAsync(
            user.MoodleUrl, user.MoodleToken, cancellationToken);

        var summaries = new List<CourseGradeSummaryDto>();
        foreach (MoodleCourseDto course in courses)
        {
            CourseGradeSummaryDto summary = await GetCourseGradeSummaryAsync(
                user.MoodleUrl, user.MoodleToken, course.Id, cancellationToken);

            // Only include courses that have grade items
            if (summary.TotalItemCount > 0)
                summaries.Add(summary);
        }

        return Result<IReadOnlyList<CourseGradeSummaryDto>>.Success(summaries);
    }

    private async Task<CourseGradeSummaryDto> GetCourseGradeSummaryAsync(
        string moodleUrl, string moodleToken, int courseId, CancellationToken cancellationToken)
    {
        MoodleGradeReportDto report = await moodleService.GetGradesAsync(
            moodleUrl, moodleToken, courseId, cancellationToken);

        MoodleUserGradeDto? userGrade = report.UserGrades.FirstOrDefault();

        if (userGrade is null)
        {
            return new CourseGradeSummaryDto
            {
                CourseId = courseId,
                CourseName = string.Empty,
            };
        }

        // Separate course total from individual items
        MoodleGradeItemDto? courseTotal = userGrade.GradeItems
            .FirstOrDefault(g => g.ItemType == "course");

        List<GradeViewDto> items = userGrade.GradeItems
            .Where(g => g.ItemType != "course" && g.ItemType != "category")
            .Select(g => new GradeViewDto
            {
                Id = g.Id,
                CourseId = courseId,
                CourseName = userGrade.UserFullName,
                ItemName = g.ItemName,
                ItemType = g.ItemType,
                ItemModule = g.ItemModule,
                Grade = g.GradeRaw,
                GradeFormatted = g.GradeFormatted,
                GradeMin = g.GradeMin,
                GradeMax = g.GradeMax,
                Percentage = g.PercentageFormatted,
                Feedback = g.Feedback,
                Weight = g.WeightFormatted,
            })
            .ToList();

        return new CourseGradeSummaryDto
        {
            CourseId = courseId,
            CourseName = userGrade.UserFullName,
            CourseTotal = courseTotal?.GradeFormatted,
            CourseTotalRaw = courseTotal?.GradeRaw,
            CourseTotalMax = courseTotal?.GradeMax ?? 100,
            Percentage = courseTotal?.PercentageFormatted,
            GradedItemCount = items.Count(i => i.Grade.HasValue),
            TotalItemCount = items.Count,
            Items = items,
        };
    }
}
