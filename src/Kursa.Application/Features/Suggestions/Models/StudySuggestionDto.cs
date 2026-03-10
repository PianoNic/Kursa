namespace Kursa.Application.Features.Suggestions.Models;

public sealed record StudySuggestionDto
{
    public string Type { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string? ActionUrl { get; init; }
    public string Priority { get; init; } = "medium";
}
