namespace SimPle.Application.Common.Options;

public sealed class AwsOptions
{
    public const string SectionName = "AWS";

    public string AccessKeyId { get; set; } = string.Empty;
    public string SecretAccessKey { get; set; } = string.Empty;
    public string Region { get; set; } = "us-east-1";
    public string S3BucketName { get; set; } = string.Empty;
    public string S3ProfilePrefix { get; set; } = "profile-assets";
    public int S3UploadUrlExpiryMinutes { get; set; } = 10;
    public int S3ReadUrlExpiryMinutes { get; set; } = 15;
}
