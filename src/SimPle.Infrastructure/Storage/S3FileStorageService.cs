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
    private readonly AwsOptions _options;

    public S3FileStorageService(IOptions<AwsOptions> options)
    {
        _options = options.Value;
        var region = RegionEndpoint.GetBySystemName(_options.Region);
        _client = string.IsNullOrWhiteSpace(_options.AccessKeyId)
            ? new AmazonS3Client(region)
            : new AmazonS3Client(_options.AccessKeyId, _options.SecretAccessKey, region);
    }

    public Task<string> CreatePresignedPutUrlAsync(
        string objectKey,
        string contentType,
        TimeSpan expiresIn,
        CancellationToken ct = default)
    {
        var request = new PutObjectRequest
        {
            BucketName = _options.S3BucketName,
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
            BucketName = _options.S3BucketName,
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
                BucketName = _options.S3BucketName,
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
            BucketName = _options.S3BucketName,
            Key = objectKey,
        }, ct);
    }
}
