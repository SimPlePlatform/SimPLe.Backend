namespace SimPle.Application.Common.Interfaces;

public interface IFileStorageService
{
    Task<string> CreatePresignedPutUrlAsync(
        string objectKey,
        string contentType,
        TimeSpan expiresIn,
        CancellationToken ct = default);

    Task<string> CreatePresignedReadUrlAsync(
        string objectKey,
        TimeSpan expiresIn,
        CancellationToken ct = default);

    Task<bool> ObjectExistsAsync(string objectKey, CancellationToken ct = default);

    Task DeleteObjectAsync(string objectKey, CancellationToken ct = default);
}
