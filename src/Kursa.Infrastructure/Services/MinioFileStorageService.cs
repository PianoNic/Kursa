using Kursa.Application.Common.Interfaces;
using Kursa.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;

namespace Kursa.Infrastructure.Services;

public sealed class MinioFileStorageService : IFileStorageService
{
    private readonly IMinioClient _client;
    private readonly string _bucketName;
    private readonly ILogger<MinioFileStorageService> _logger;

    public MinioFileStorageService(IOptions<MinIOOptions> options, ILogger<MinioFileStorageService> logger)
    {
        _logger = logger;
        MinIOOptions opts = options.Value;
        _bucketName = opts.BucketName;

        var builder = new MinioClient()
            .WithEndpoint(opts.Endpoint)
            .WithCredentials(opts.AccessKey, opts.SecretKey);

        if (opts.UseSsl)
            builder = builder.WithSSL();

        _client = builder.Build();
    }

    public async Task<string> UploadAsync(
        string objectKey,
        Stream stream,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        await EnsureBucketExistsAsync(cancellationToken);

        var args = new PutObjectArgs()
            .WithBucket(_bucketName)
            .WithObject(objectKey)
            .WithStreamData(stream)
            .WithObjectSize(stream.Length)
            .WithContentType(contentType);

        await _client.PutObjectAsync(args, cancellationToken);

        _logger.LogInformation("Uploaded object {ObjectKey} to bucket {Bucket}", objectKey, _bucketName);

        return objectKey;
    }

    public async Task<Stream> DownloadAsync(
        string objectKey,
        CancellationToken cancellationToken = default)
    {
        var memoryStream = new MemoryStream();

        var args = new GetObjectArgs()
            .WithBucket(_bucketName)
            .WithObject(objectKey)
            .WithCallbackStream(stream => stream.CopyTo(memoryStream));

        await _client.GetObjectAsync(args, cancellationToken);

        memoryStream.Position = 0;
        return memoryStream;
    }

    public async Task DeleteAsync(
        string objectKey,
        CancellationToken cancellationToken = default)
    {
        var args = new RemoveObjectArgs()
            .WithBucket(_bucketName)
            .WithObject(objectKey);

        await _client.RemoveObjectAsync(args, cancellationToken);

        _logger.LogInformation("Deleted object {ObjectKey} from bucket {Bucket}", objectKey, _bucketName);
    }

    public async Task<string> GetPresignedUrlAsync(
        string objectKey,
        int expirySeconds = 3600,
        CancellationToken cancellationToken = default)
    {
        var args = new PresignedGetObjectArgs()
            .WithBucket(_bucketName)
            .WithObject(objectKey)
            .WithExpiry(expirySeconds);

        return await _client.PresignedGetObjectAsync(args);
    }

    private async Task EnsureBucketExistsAsync(CancellationToken cancellationToken)
    {
        var existsArgs = new BucketExistsArgs().WithBucket(_bucketName);
        bool exists = await _client.BucketExistsAsync(existsArgs, cancellationToken);

        if (!exists)
        {
            var makeArgs = new MakeBucketArgs().WithBucket(_bucketName);
            await _client.MakeBucketAsync(makeArgs, cancellationToken);
            _logger.LogInformation("Created MinIO bucket {Bucket}", _bucketName);
        }
    }
}
