using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MMGC.Models;

/// <summary>
/// Polymorphic image model for storing images associated with various entities.
/// Use ImageableType + ImageableId + Tag to identify the owner and purpose.
/// </summary>
public class Image
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string ImageableType { get; set; } = string.Empty; // e.g. "Doctor", "Patient"

    [Required]
    public int ImageableId { get; set; }

    /// <summary>
    /// Purpose/tag for the image (e.g. "profile", "cover", "document")
    /// </summary>
    [Required]
    [StringLength(50)]
    public string Tag { get; set; } = string.Empty;

    [Required]
    [StringLength(500)]
    public string FilePath { get; set; } = string.Empty;

    [StringLength(255)]
    public string? FileName { get; set; }

    [StringLength(100)]
    public string? ContentType { get; set; }

    public long FileSizeBytes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
