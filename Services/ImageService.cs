using Microsoft.EntityFrameworkCore;
using MMGC.Data;
using MMGC.Models;
using MMGC.Shared.Interfaces;

namespace MMGC.Services;

public class ImageService : IImageService
{
    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
    private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5MB

    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<ImageService> _logger;

    public ImageService(
        ApplicationDbContext context,
        IWebHostEnvironment env,
        ILogger<ImageService> logger)
    {
        _context = context;
        _env = env;
        _logger = logger;
    }

    public async Task<string?> UploadImageAsync(
        string imageableType,
        int imageableId,
        string tag,
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
        {
            _logger.LogDebug("Image upload skipped: file null or empty");
            return null;
        }

        var ext = Path.GetExtension(file.FileName)?.ToLowerInvariant();
        if (string.IsNullOrEmpty(ext) || !AllowedExtensions.Contains(ext))
        {
            _logger.LogWarning("Rejected image upload: invalid extension {Ext}", ext);
            return null;
        }

        // Accept any image/* content type (browsers may send image/pjpeg, image/x-png, etc.)
        var contentType = file.ContentType?.ToLowerInvariant() ?? "";
        if (!contentType.StartsWith("image/"))
        {
            _logger.LogWarning("Rejected image upload: invalid content type {ContentType}", file.ContentType);
            return null;
        }

        if (file.Length > MaxFileSizeBytes)
        {
            _logger.LogWarning("Rejected image upload: file too large {Size} bytes", file.Length);
            return null;
        }

        var uploadsRoot = Path.Combine(_env.WebRootPath ?? "wwwroot", "uploads", "images");
        var typeFolder = Path.Combine(uploadsRoot, imageableType);
        if (!Directory.Exists(typeFolder))
            Directory.CreateDirectory(typeFolder);

        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var safeFileName = $"{imageableId}_{tag}_{timestamp}{ext}";
        var fullPath = Path.Combine(typeFolder, safeFileName);
        var relativePath = $"/uploads/images/{imageableType}/{safeFileName}";

        try
        {
            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream, cancellationToken);
            }

            // Remove existing image with same tag
            var existing = await _context.Images
                .FirstOrDefaultAsync(
                    i => i.ImageableType == imageableType && i.ImageableId == imageableId && i.Tag == tag,
                    cancellationToken);

            if (existing != null)
            {
                try
                {
                    var oldPath = Path.Combine(_env.WebRootPath ?? "wwwroot", existing.FilePath.TrimStart('/'));
                    if (File.Exists(oldPath))
                        File.Delete(oldPath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not delete old image file");
                }
                _context.Images.Remove(existing);
            }

            var image = new Image
            {
                ImageableType = imageableType,
                ImageableId = imageableId,
                Tag = tag,
                FilePath = relativePath,
                FileName = file.FileName,
                ContentType = file.ContentType,
                FileSizeBytes = file.Length,
                CreatedAt = DateTime.UtcNow
            };
            _context.Images.Add(image);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Image saved for {Type}/{Id}/{Tag}: {Path}", imageableType, imageableId, tag, relativePath);
            return relativePath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading image for {Type}/{Id}/{Tag}", imageableType, imageableId, tag);
            if (File.Exists(fullPath))
                File.Delete(fullPath);
            return null;
        }
    }

    public async Task<string?> GetImageUrlAsync(
        string imageableType,
        int imageableId,
        string tag,
        CancellationToken cancellationToken = default)
    {
        var img = await _context.Images
            .AsNoTracking()
            .FirstOrDefaultAsync(
                i => i.ImageableType == imageableType && i.ImageableId == imageableId && i.Tag == tag,
                cancellationToken);
        return img?.FilePath;
    }

    public async Task DeleteImageAsync(
        string imageableType,
        int imageableId,
        string tag,
        CancellationToken cancellationToken = default)
    {
        var img = await _context.Images
            .FirstOrDefaultAsync(
                i => i.ImageableType == imageableType && i.ImageableId == imageableId && i.Tag == tag,
                cancellationToken);
        if (img == null) return;

        try
        {
            var fullPath = Path.Combine(_env.WebRootPath ?? "wwwroot", img.FilePath.TrimStart('/'));
            if (File.Exists(fullPath))
                File.Delete(fullPath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not delete image file");
        }

        _context.Images.Remove(img);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
