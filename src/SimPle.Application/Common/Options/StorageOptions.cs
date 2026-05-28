namespace SimPle.Application.Common.Options;

public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    public string Provider { get; set; } = "AWS";
    public string BucketName { get; set; } = string.Empty;
    public string Region { get; set; } = "us-east-1";
    public string? ServiceUrl { get; set; }
    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string ProfilePrefix { get; set; } = "profile-assets";
    public bool ForcePathStyle { get; set; }
    public int UploadUrlExpiryMinutes { get; set; } = 5;
    public int ReadUrlExpiryMinutes { get; set; } = 30;
}
