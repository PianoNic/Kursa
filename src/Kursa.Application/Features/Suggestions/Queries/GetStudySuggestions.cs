using Kursa.Application.Common.Interfaces;
using Kursa.Application.Common.Models;
using Kursa.Application.Features.Suggestions.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Kursa.Application.Features.Suggestions.Queries;

public sealed record GetStudySuggestionsQuery : IRequest<Result<IReadOnlyList<StudySuggestionDto>>>;

public sealed class GetStudySuggestionsHandler(
    ICurrentUserService currentUserService,
    IAppDbContext dbContext,
    IStudySuggestionService suggestionService) : IRequestHandler<GetStudySuggestionsQuery, Result<IReadOnlyList<StudySuggestionDto>>>
{
    public async Task<Result<IReadOnlyList<StudySuggestionDto>>> Handle(
        GetStudySuggestionsQuery request, CancellationToken cancellationToken)
    {
        if (!currentUserService.IsAuthenticated || currentUserService.ExternalId is null)
            return Result<IReadOnlyList<StudySuggestionDto>>.Failure("User is not authenticated.");

        var user = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.ExternalId == currentUserService.ExternalId, cancellationToken);

        if (user is null)
            return Result<IReadOnlyList<StudySuggestionDto>>.Failure("User not found.");

        IReadOnlyList<StudySuggestionDto> suggestions = await suggestionService
            .GenerateSuggestionsAsync(user.Id, cancellationToken);

        return Result<IReadOnlyList<StudySuggestionDto>>.Success(suggestions);
    }
}
