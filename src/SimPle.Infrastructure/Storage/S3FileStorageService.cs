using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using SimPle.Application.Common.Interfaces;
using SimPle.Application.Common.Options;

namespace SimPle.Infrastructure.Storage;

public sealed class S3FileStorageService : IFileStorageService
{
    private readonly IAmazonS3 _client;
    private readonly StorageOptions _options;

    public S3FileStorageService(IOptions<StorageOptions> options) : this(options.Value, null)
    {
    }

    public S3FileStorageService(StorageOptions options, IAmazonS3? client)
    {
        _options = options;
        var config = BuildClientConfig(_options);
        _client = client ?? (string.IsNullOrWhiteSpace(_options.AccessKey)
            ? new AmazonS3Client(config)
            : new AmazonS3Client(_options.AccessKey, _options.SecretKey, config));
    }

    public Task<string> CreatePresignedPutUrlAsync(
        string objectKey,
        string contentType,
        TimeSpan expiresIn,
        CancellationToken ct = default)
    {
        var request = new PutObjectRequest
        {
            BucketName = _options.BucketName,
            Key = objectKey,
            ContentType = contentType,
        };

        var url = _client.GetPreSignedURL(new GetPreSignedUrlRequest
        {
            BucketName = request.BucketName,
            Key = request.Key,
            Verb = HttpVerb.PUT,
            ContentType = request.ContentType,
            Expires = DateTime.UtcNow.Add(expiresIn)
        });

        return Task.FromResult(url);
    }

    public Task<string> CreatePresignedReadUrlAsync(
        string objectKey,
        TimeSpan expiresIn,
        CancellationToken ct = default)
    {
        var url = _client.GetPreSignedURL(new GetPreSignedUrlRequest
        {
            BucketName = _options.BucketName,
            Key = objectKey,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.Add(expiresIn)
        });

        return Task.FromResult(url);
    }

    public async Task<bool> ObjectExistsAsync(string objectKey, CancellationToken ct = default)
    {
        try
        {
            await _client.GetObjectMetadataAsync(new GetObjectMetadataRequest
            {
                BucketName = _options.BucketName,
                Key = objectKey,
            }, ct);
            return true;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    public async Task DeleteObjectAsync(string objectKey, CancellationToken ct = default)
    {
        await _client.DeleteObjectAsync(new DeleteObjectRequest
        {
            BucketName = _options.BucketName,
            Key = objectKey,
        }, ct);
    }

    public static AmazonS3Config BuildClientConfig(StorageOptions options)
    {
        var config = new AmazonS3Config
        {
            RegionEndpoint = RegionEndpoint.GetBySystemName(options.Region),
            ForcePathStyle = options.ForcePathStyle,
        };

        if (!string.IsNullOrWhiteSpace(options.ServiceUrl))
        {
            config.ServiceURL = options.ServiceUrl;
            config.AuthenticationRegion = options.Region;
        }

        return config;
    }
}
