namespace SimPle.Application.Common.Options;

public sealed class AwsOptions
{
    public const string SectionName = "AWS";

    public string AccessKeyId { get; init; } = string.Empty;
    public string SecretAccessKey { get; init; } = string.Empty;
    public string Region { get; init; } = "us-east-1";
    public string S3BucketName { get; init; } = string.Empty;
    /// <summary>Optional custom public URL prefix (e.g. CloudFront domain). Defaults to S3 path-style URL.</summary>
    public string? S3PublicUrlBase { get; init; }
}
