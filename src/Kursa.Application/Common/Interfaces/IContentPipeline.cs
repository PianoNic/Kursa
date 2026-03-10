namespace Kursa.Application.Common.Interfaces;

public interface IContentPipeline
{
    Task IndexContentAsync(Guid contentId, Guid userId, CancellationToken cancellationToken = default);

    Task RemoveContentIndexAsync(Guid contentId, CancellationToken cancellationToken = default);
}
