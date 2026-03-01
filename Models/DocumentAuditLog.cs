using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MMGC.Models;

/// <summary>
/// Audit log for tracking PDF document generation for compliance and security.
/// Records who generated what document and when.
/// </summary>
public class DocumentAuditLog
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Type of document generated (Prescription, Invoice, LabReport, ProcedureReport, etc.).
    /// </summary>
    [Required]
    [StringLength(100)]
    public string DocumentType { get; set; } = string.Empty;

    /// <summary>
    /// ID of the entity the document was generated for.
    /// </summary>
    [Required]
    public int EntityId { get; set; }

    /// <summary>
    /// Patient associated with the document.
    /// </summary>
    public int? PatientId { get; set; }

    /// <summary>
    /// User who requested the document generation.
    /// </summary>
    [Required]
    [StringLength(450)]
    public string RequestedBy { get; set; } = string.Empty;

    /// <summary>
    /// Generated filename.
    /// </summary>
    [Required]
    [StringLength(255)]
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// File size in bytes.
    /// </summary>
    public long FileSizeBytes { get; set; }

    /// <summary>
    /// SHA256 hash of the generated PDF for integrity verification.
    /// </summary>
    [StringLength(64)]
    public string? FileHash { get; set; }

    /// <summary>
    /// Timestamp when the document was generated.
    /// </summary>
    [Required]
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// IP address of the requester.
    /// </summary>
    [StringLength(50)]
    public string? RequestorIpAddress { get; set; }

    /// <summary>
    /// Whether the document was downloaded by the user.
    /// </summary>
    public bool WasDownloaded { get; set; } = false;

    /// <summary>
    /// Timestamp when document was downloaded (if applicable).
    /// </summary>
    public DateTime? DownloadedAt { get; set; }

    /// <summary>
    /// Generation status (Success, Failed).
    /// </summary>
    [StringLength(50)]
    public string Status { get; set; } = "Success";

    /// <summary>
    /// Error message if generation failed.
    /// </summary>
    [StringLength(500)]
    public string? ErrorMessage { get; set; }

    // Navigation properties
    [ForeignKey(nameof(PatientId))]
    public virtual Patient? Patient { get; set; }
}
