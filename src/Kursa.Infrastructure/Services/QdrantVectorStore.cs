using Kursa.Application.Common.Interfaces;
using Kursa.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Qdrant.Client;
using Qdrant.Client.Grpc;
using static Qdrant.Client.Grpc.Conditions;

namespace Kursa.Infrastructure.Services;

public sealed class QdrantVectorStore : IVectorStore
{
    private readonly QdrantClient _client;
    private readonly ILogger<QdrantVectorStore> _logger;

    public QdrantVectorStore(IOptions<QdrantOptions> options, ILogger<QdrantVectorStore> logger)
    {
        _logger = logger;
        QdrantOptions qdrantOptions = options.Value;

        _client = string.IsNullOrEmpty(qdrantOptions.ApiKey)
            ? new QdrantClient(qdrantOptions.Host, qdrantOptions.Port)
            : new QdrantClient(qdrantOptions.Host, qdrantOptions.Port, apiKey: qdrantOptions.ApiKey);
    }

    public async Task EnsureCollectionAsync(string collectionName, int vectorSize, CancellationToken cancellationToken = default)
    {
        try
        {
            bool exists = await _client.CollectionExistsAsync(collectionName, cancellationToken);
            if (exists) return;

            await _client.CreateCollectionAsync(
                collectionName,
                new VectorParams { Size = (ulong)vectorSize, Distance = Distance.Cosine },
                cancellationToken: cancellationToken);

            _logger.LogInformation("Created Qdrant collection '{Collection}' with vector size {Size}", collectionName, vectorSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ensure Qdrant collection '{Collection}'", collectionName);
            throw;
        }
    }

    public async Task UpsertAsync(string collectionName, IReadOnlyList<VectorPoint> points, CancellationToken cancellationToken = default)
    {
        if (points.Count == 0) return;

        var qdrantPoints = points.Select(p => new PointStruct
        {
            Id = p.Id,
            Vectors = p.Vector.ToArray(),
            Payload =
            {
                ["content_id"] = p.ContentId.ToString(),
                ["user_id"] = p.UserId.ToString(),
                ["chunk_text"] = p.ChunkText,
                ["chunk_index"] = p.ChunkIndex,
                ["content_title"] = p.ContentTitle ?? string.Empty,
                ["content_type"] = p.ContentType ?? string.Empty,
                ["source_url"] = p.SourceUrl ?? string.Empty,
            }
        }).ToList();

        await _client.UpsertAsync(collectionName, qdrantPoints, cancellationToken: cancellationToken);
        _logger.LogDebug("Upserted {Count} points to collection '{Collection}'", points.Count, collectionName);
    }

    public async Task<IReadOnlyList<VectorSearchResult>> SearchAsync(
        string collectionName,
        IReadOnlyList<float> queryVector,
        int limit = 5,
        Guid? filterByUserId = null,
        Guid? filterByContentId = null,
        CancellationToken cancellationToken = default)
    {
        Filter? filter = null;

        if (filterByUserId.HasValue && filterByContentId.HasValue)
        {
            filter = MatchKeyword("user_id", filterByUserId.Value.ToString())
                   & MatchKeyword("content_id", filterByContentId.Value.ToString());
        }
        else if (filterByUserId.HasValue)
        {
            filter = MatchKeyword("user_id", filterByUserId.Value.ToString());
        }
        else if (filterByContentId.HasValue)
        {
            filter = MatchKeyword("content_id", filterByContentId.Value.ToString());
        }

        IReadOnlyList<ScoredPoint> results;
        try
        {
            results = await _client.SearchAsync(
                collectionName,
                queryVector.ToArray(),
                filter: filter,
                limit: (ulong)limit,
                cancellationToken: cancellationToken);
        }
        catch (Grpc.Core.RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.NotFound)
        {
            _logger.LogDebug("Qdrant collection '{Collection}' does not exist yet — returning empty results", collectionName);
            return [];
        }

        return results.Select(r => new VectorSearchResult
        {
            Id = r.Id.Uuid != null ? Guid.Parse(r.Id.Uuid) : Guid.Empty,
            Score = r.Score,
            ContentId = Guid.TryParse(GetPayloadString(r, "content_id"), out Guid cid) ? cid : Guid.Empty,
            UserId = Guid.TryParse(GetPayloadString(r, "user_id"), out Guid uid) ? uid : Guid.Empty,
            ChunkText = GetPayloadString(r, "chunk_text"),
            ChunkIndex = (int)(r.Payload.TryGetValue("chunk_index", out Value? ci) ? ci.IntegerValue : 0),
            ContentTitle = GetPayloadString(r, "content_title"),
            ContentType = GetPayloadString(r, "content_type"),
            SourceUrl = GetPayloadString(r, "source_url"),
        }).ToList();
    }

    public async Task DeleteByContentIdAsync(string collectionName, Guid contentId, CancellationToken cancellationToken = default)
    {
        Filter filter = MatchKeyword("content_id", contentId.ToString());
        await _client.DeleteAsync(collectionName, filter, cancellationToken: cancellationToken);
        _logger.LogInformation("Deleted vectors for content {ContentId} from collection '{Collection}'", contentId, collectionName);
    }

    private static string GetPayloadString(ScoredPoint point, string key)
    {
        return point.Payload.TryGetValue(key, out Value? value) ? value.StringValue : string.Empty;
    }
}
