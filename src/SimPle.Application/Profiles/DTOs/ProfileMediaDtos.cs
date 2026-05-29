namespace SimPle.Application.Profiles.DTOs;

public sealed record ProfileMediaUploadUrlRequestDto(
    string FileName,
    string ContentType,
    long FileSizeBytes);

public sealed record ProfileMediaUploadUrlDto(
    string UploadUrl,
    string ObjectKey,
    string ContentType,
    DateTime ExpiresAtUtc);

public sealed record ConfirmProfileMediaUploadRequestDto(string ObjectKey);

public sealed record UpdateAvatarFallbackRequestDto(string Color);

public sealed record UpdateBannerFallbackRequestDto(string Color);
