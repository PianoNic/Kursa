using Kursa.Application.Common.Interfaces;
using Kursa.Application.Common.Models;
using Kursa.Application.Features.Moodle.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Kursa.Application.Features.Moodle.Queries;

public sealed record GetEnrolledCoursesQuery : IRequest<Result<IReadOnlyList<MoodleCourseDto>>>;

public sealed class GetEnrolledCoursesHandler(
    ICurrentUserService currentUserService,
    IAppDbContext dbContext,
    IMoodleService moodleService) : IRequestHandler<GetEnrolledCoursesQuery, Result<IReadOnlyList<MoodleCourseDto>>>
{
    public async Task<Result<IReadOnlyList<MoodleCourseDto>>> Handle(
        GetEnrolledCoursesQuery request, CancellationToken cancellationToken)
    {
        if (!currentUserService.IsAuthenticated || currentUserService.ExternalId is null)
            return Result<IReadOnlyList<MoodleCourseDto>>.Failure("User is not authenticated.");

        var user = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.ExternalId == currentUserService.ExternalId, cancellationToken);

        if (user is null)
            return Result<IReadOnlyList<MoodleCourseDto>>.Failure("User not found.");

        if (string.IsNullOrEmpty(user.MoodleToken))
            return Result<IReadOnlyList<MoodleCourseDto>>.Failure("Moodle account is not linked.");

        var courses = await moodleService.GetEnrolledCoursesAsync(
            user.MoodleToken, cancellationToken);

        return Result<IReadOnlyList<MoodleCourseDto>>.Success(courses);
    }
}
