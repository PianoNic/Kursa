using Kursa.Application.Features.Suggestions.Models;

namespace Kursa.Application.Common.Interfaces;

public interface IStudySuggestionService
{
    Task<IReadOnlyList<StudySuggestionDto>> GenerateSuggestionsAsync(
        Guid userId, CancellationToken cancellationToken = default);
}
