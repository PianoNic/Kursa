using Kursa.Application.Features.Moodle.Commands;
using Kursa.Application.Features.Moodle.Models;
using Kursa.Application.Features.Moodle.Queries;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kursa.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MoodleController(ISender sender) : ControllerBase
{
    [HttpGet("status")]
    [ProducesResponseType(typeof(MoodleConnectionStatusDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetConnectionStatusAsync(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetMoodleConnectionStatusQuery(), cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(result.Error);
    }

    /// <summary>
    /// Validates Moodle credentials without storing them. Used during onboarding before user is created.
    /// </summary>
    [HttpPost("validate")]
    public async Task<IActionResult> ValidateCredentialsAsync(
        ValidateMoodleCredentialsCommand command,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);

        return result.IsSuccess
            ? Ok()
            : BadRequest(result.Error);
    }

    [HttpPost("link")]
    public async Task<IActionResult> LinkTokenAsync(
        LinkMoodleTokenCommand command,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);

        return result.IsSuccess
            ? Ok()
            : BadRequest(result.Error);
    }

    [HttpDelete("link")]
    public async Task<IActionResult> UnlinkTokenAsync(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new UnlinkMoodleTokenCommand(), cancellationToken);

        return result.IsSuccess
            ? Ok()
            : BadRequest(result.Error);
    }

    [HttpGet("courses")]
    [ProducesResponseType(typeof(IReadOnlyList<MoodleCourseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCoursesAsync(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetEnrolledCoursesQuery(), cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(result.Error);
    }

    [HttpGet("courses/{courseId:int}/content")]
    [ProducesResponseType(typeof(IReadOnlyList<MoodleCourseSectionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCourseContentAsync(int courseId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetCourseContentQuery(courseId), cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(result.Error);
    }

    [HttpGet("assignments")]
    [ProducesResponseType(typeof(IReadOnlyList<AssignmentViewDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAssignmentsAsync(
        [FromQuery] int? courseId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetAssignmentsQuery(courseId), cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(result.Error);
    }

    [HttpGet("grades")]
    [ProducesResponseType(typeof(IReadOnlyList<CourseGradeSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetGradesAsync(
        [FromQuery] int? courseId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetGradesQuery(courseId), cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(result.Error);
    }

    [HttpGet("courses/{courseId:int}/forums")]
    [ProducesResponseType(typeof(IReadOnlyList<ForumViewDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetForumsAsync(int courseId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetForumsQuery(courseId), cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(result.Error);
    }

    [HttpGet("forums/{forumId:int}/discussions")]
    [ProducesResponseType(typeof(IReadOnlyList<DiscussionViewDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetForumDiscussionsAsync(int forumId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetForumDiscussionsQuery(forumId), cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(result.Error);
    }

    [HttpGet("calendar")]
    [ProducesResponseType(typeof(IReadOnlyList<CalendarEventViewDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCalendarEventsAsync(
        [FromQuery] DateTime weekStart, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetCalendarEventsQuery(weekStart), cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(result.Error);
    }
}
