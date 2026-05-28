namespace SimPle.Application.Common.Interfaces;

public interface IFileStorageService
{
    /// <summary>
    /// Uploads a stream to storage and returns the public URL.
    /// </summary>
    Task<string> UploadAsync(
        Stream content,
        string fileName,
        string contentType,
        CancellationToken ct = default);

    /// <summary>
    /// Deletes an object by its full public URL. No-op if the URL is not in this bucket.
    /// </summary>
    Task DeleteAsync(string publicUrl, CancellationToken ct = default);
}
