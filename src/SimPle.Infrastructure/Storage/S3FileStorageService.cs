using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using SimPle.Application.Common.Interfaces;
using SimPle.Application.Common.Options;

namespace SimPle.Infrastructure.Storage;

public sealed class S3FileStorageService : IFileStorageService
{
    private readonly AmazonS3Client _client;
    private readonly AwsOptions _options;

    public S3FileStorageService(IOptions<AwsOptions> options)
    {
        _options = options.Value;
        _client = new AmazonS3Client(
            _options.AccessKeyId,
            _options.SecretAccessKey,
            RegionEndpoint.GetBySystemName(_options.Region));
    }

    public async Task<string> UploadAsync(
        Stream content, string fileName, string contentType, CancellationToken ct = default)
    {
        var request = new PutObjectRequest
        {
            BucketName = _options.S3BucketName,
            Key = fileName,
            InputStream = content,
            ContentType = contentType,
            CannedACL = S3CannedACL.PublicRead,
        };

        await _client.PutObjectAsync(request, ct);
        return BuildPublicUrl(fileName);
    }

    public async Task DeleteAsync(string publicUrl, CancellationToken ct = default)
    {
        var key = ExtractKey(publicUrl);
        if (key is null) return;

        await _client.DeleteObjectAsync(new DeleteObjectRequest
        {
            BucketName = _options.S3BucketName,
            Key = key,
        }, ct);
    }

    private string BuildPublicUrl(string key)
    {
        if (!string.IsNullOrEmpty(_options.S3PublicUrlBase))
            return $"{_options.S3PublicUrlBase.TrimEnd('/')}/{key}";

        return $"https://{_options.S3BucketName}.s3.{_options.Region}.amazonaws.com/{key}";
    }

    private string? ExtractKey(string publicUrl)
    {
        if (!string.IsNullOrEmpty(_options.S3PublicUrlBase) &&
            publicUrl.StartsWith(_options.S3PublicUrlBase, StringComparison.OrdinalIgnoreCase))
            return publicUrl[(publicUrl.LastIndexOf('/') + 1)..];

        var expectedPrefix = $"https://{_options.S3BucketName}.s3";
        if (!publicUrl.Contains(_options.S3BucketName)) return null;
        var lastSlash = publicUrl.LastIndexOf('/');
        return lastSlash >= 0 ? publicUrl[(lastSlash + 1)..] : null;
    }
}
