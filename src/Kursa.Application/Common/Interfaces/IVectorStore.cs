namespace Kursa.Application.Common.Interfaces;

public interface IVectorStore
{
    Task EnsureCollectionAsync(string collectionName, int vectorSize, CancellationToken cancellationToken = default);

    Task UpsertAsync(string collectionName, IReadOnlyList<VectorPoint> points, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<VectorSearchResult>> SearchAsync(
        string collectionName,
        IReadOnlyList<float> queryVector,
        int limit = 5,
        Guid? filterByUserId = null,
        Guid? filterByContentId = null,
        CancellationToken cancellationToken = default);

    Task DeleteByContentIdAsync(string collectionName, Guid contentId, CancellationToken cancellationToken = default);
}

public sealed record VectorPoint
{
    public required Guid Id { get; init; }
    public required IReadOnlyList<float> Vector { get; init; }
    public required Guid ContentId { get; init; }
    public required Guid UserId { get; init; }
    public required string ChunkText { get; init; }
    public required int ChunkIndex { get; init; }
    public string? ContentTitle { get; init; }
    public string? ContentType { get; init; }
    public string? SourceUrl { get; init; }
}

public sealed record VectorSearchResult
{
    public required Guid Id { get; init; }
    public required float Score { get; init; }
    public required Guid ContentId { get; init; }
    public required Guid UserId { get; init; }
    public required string ChunkText { get; init; }
    public required int ChunkIndex { get; init; }
    public string? ContentTitle { get; init; }
    public string? ContentType { get; init; }
    public string? SourceUrl { get; init; }
}
