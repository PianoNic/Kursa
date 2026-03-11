using Kursa.Application.Features.Moodle.Models;

namespace Kursa.Application.Common.Interfaces;

public interface IMoodleService
{
    /// <summary>Authenticates with MoodlewareAPI using Moodle credentials and returns the wstoken, or null on failure.</summary>
    Task<string?> GetTokenAsync(string username, string password, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MoodleCourseDto>> GetEnrolledCoursesAsync(
        string moodleToken, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MoodleCourseSectionDto>> GetCourseContentAsync(
        string moodleToken, int courseId, CancellationToken cancellationToken = default);

    Task<MoodleSiteInfoDto> GetSiteInfoAsync(
        string moodleToken, CancellationToken cancellationToken = default);

    Task<MoodleAssignmentsResponseDto> GetAssignmentsAsync(
        string moodleToken, IReadOnlyList<int>? courseIds = null,
        CancellationToken cancellationToken = default);

    Task<MoodleGradeReportDto> GetGradesAsync(
        string moodleToken, int courseId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MoodleForumDto>> GetForumsAsync(
        string moodleToken, int courseId, CancellationToken cancellationToken = default);

    Task<MoodleForumDiscussionsResponseDto> GetForumDiscussionsAsync(
        string moodleToken, int forumId, CancellationToken cancellationToken = default);

    Task<MoodleCalendarEventsResponseDto> GetCalendarEventsAsync(
        string moodleToken, long timeStart, long timeEnd,
        CancellationToken cancellationToken = default);
}
