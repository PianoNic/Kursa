namespace Kursa.Application.Common.Interfaces;

public interface ISummaryService
{
    Task<string> GenerateSummaryAsync(Guid contentId, Guid userId, CancellationToken cancellationToken = default);
}
