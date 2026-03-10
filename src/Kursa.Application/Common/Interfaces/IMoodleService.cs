using Kursa.Application.Features.Moodle.Models;

namespace Kursa.Application.Common.Interfaces;

public interface IMoodleService
{
    Task<IReadOnlyList<MoodleCourseDto>> GetEnrolledCoursesAsync(
        string moodleUrl, string moodleToken, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MoodleCourseSectionDto>> GetCourseContentAsync(
        string moodleUrl, string moodleToken, int courseId, CancellationToken cancellationToken = default);

    Task<MoodleSiteInfoDto> GetSiteInfoAsync(
        string moodleUrl, string moodleToken, CancellationToken cancellationToken = default);

    Task<MoodleAssignmentsResponseDto> GetAssignmentsAsync(
        string moodleUrl, string moodleToken, IReadOnlyList<int>? courseIds = null,
        CancellationToken cancellationToken = default);
}
