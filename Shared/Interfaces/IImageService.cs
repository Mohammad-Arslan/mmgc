namespace MMGC.Shared.Interfaces;

/// <summary>
/// Service for uploading and managing polymorphic images.
/// </summary>
public interface IImageService
{
    /// <summary>
    /// Uploads an image for an entity. Replaces existing image with same tag.
    /// </summary>
    /// <param name="imageableType">Entity type (e.g. "Doctor")</param>
    /// <param name="imageableId">Entity ID</param>
    /// <param name="tag">Image purpose (e.g. "profile")</param>
    /// <param name="file">Uploaded file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Relative URL path (e.g. /uploads/images/Doctor/1_profile_xxx.jpg) or null if failed</returns>
    Task<string?> UploadImageAsync(
        string imageableType,
        int imageableId,
        string tag,
        IFormFile file,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the URL for an entity's image with the given tag.
    /// </summary>
    Task<string?> GetImageUrlAsync(
        string imageableType,
        int imageableId,
        string tag,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the image for an entity with the given tag.
    /// </summary>
    Task DeleteImageAsync(
        string imageableType,
        int imageableId,
        string tag,
        CancellationToken cancellationToken = default);
}
