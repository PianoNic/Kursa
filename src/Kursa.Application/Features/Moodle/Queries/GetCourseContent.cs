using FluentValidation;
using Kursa.Application.Common.Interfaces;
using Kursa.Application.Common.Models;
using Kursa.Application.Features.Moodle.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Kursa.Application.Features.Moodle.Queries;

public sealed record GetCourseContentQuery(int MoodleCourseId) : IRequest<Result<IReadOnlyList<MoodleCourseSectionDto>>>;

public sealed class GetCourseContentValidator : AbstractValidator<GetCourseContentQuery>
{
    public GetCourseContentValidator()
    {
        RuleFor(x => x.MoodleCourseId).GreaterThan(0).WithMessage("Course ID must be positive.");
    }
}

public sealed class GetCourseContentHandler(
    ICurrentUserService currentUserService,
    IAppDbContext dbContext,
    IMoodleService moodleService) : IRequestHandler<GetCourseContentQuery, Result<IReadOnlyList<MoodleCourseSectionDto>>>
{
    public async Task<Result<IReadOnlyList<MoodleCourseSectionDto>>> Handle(
        GetCourseContentQuery request, CancellationToken cancellationToken)
    {
        if (!currentUserService.IsAuthenticated || currentUserService.ExternalId is null)
            return Result<IReadOnlyList<MoodleCourseSectionDto>>.Failure("User is not authenticated.");

        var user = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.ExternalId == currentUserService.ExternalId, cancellationToken);

        if (user is null)
            return Result<IReadOnlyList<MoodleCourseSectionDto>>.Failure("User not found.");

        if (string.IsNullOrEmpty(user.MoodleToken))
            return Result<IReadOnlyList<MoodleCourseSectionDto>>.Failure("Moodle account is not linked.");

        var sections = await moodleService.GetCourseContentAsync(
            user.MoodleToken, request.MoodleCourseId, cancellationToken);

        return Result<IReadOnlyList<MoodleCourseSectionDto>>.Success(sections);
    }
}
