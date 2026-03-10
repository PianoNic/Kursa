namespace Kursa.Application.Common.Interfaces;

public interface IFileStorageService
{
    Task<string> UploadAsync(
        string objectKey,
        Stream stream,
        string contentType,
        CancellationToken cancellationToken = default);

    Task<Stream> DownloadAsync(
        string objectKey,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        string objectKey,
        CancellationToken cancellationToken = default);

    Task<string> GetPresignedUrlAsync(
        string objectKey,
        int expirySeconds = 3600,
        CancellationToken cancellationToken = default);
}
