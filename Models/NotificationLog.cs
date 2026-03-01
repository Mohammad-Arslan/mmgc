using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MMGC.Shared.Enums;

namespace MMGC.Models;

/// <summary>
/// Audit log for tracking notification delivery and status.
/// Used for notification delivery confirmation and retry management.
/// </summary>
public class NotificationLog
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Unique identifier for tracking this specific notification.
    /// </summary>
    [Required]
    [StringLength(100)]
    public string NotificationId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Recipient's contact information (phone for SMS, email for Email, etc.).
    /// </summary>
    [Required]
    [StringLength(255)]
    public string RecipientContact { get; set; } = string.Empty;

    /// <summary>
    /// Type of notification sent (SMS, Email, WhatsApp).
    /// </summary>
    [Required]
    [StringLength(50)]
    public string NotificationType { get; set; } = string.Empty;

    /// <summary>
    /// Notification type enum value.
    /// </summary>
    [Required]
    public NotificationTypeEnum NotificationTypeEnum { get; set; }

    /// <summary>
    /// Message content that was sent.
    /// </summary>
    [Required]
    [StringLength(2000)]
    public string MessageContent { get; set; } = string.Empty;

    /// <summary>
    /// Current status of notification (Pending, Sent, Delivered, Failed).
    /// </summary>
    [Required]
    [StringLength(50)]
    public string Status { get; set; } = "Pending";

    /// <summary>
    /// If delivery failed, the error message.
    /// </summary>
    [StringLength(500)]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Number of times delivery was attempted.
    /// </summary>
    public int RetryCount { get; set; } = 0;

    /// <summary>
    /// Optional: External service ID (from Twilio, SendGrid, etc.).
    /// </summary>
    [StringLength(100)]
    public string? ExternalMessageId { get; set; }

    /// <summary>
    /// Associated patient ID (for audit trail).
    /// </summary>
    public int? PatientId { get; set; }

    /// <summary>
    /// Associated appointment ID (if applicable).
    /// </summary>
    public int? AppointmentId { get; set; }

    /// <summary>
    /// Associated procedure request ID (if applicable).
    /// </summary>
    public int? ProcedureRequestId { get; set; }

    /// <summary>
    /// Associated transaction ID (if applicable).
    /// </summary>
    public int? TransactionId { get; set; }

    /// <summary>
    /// Associated lab test ID (if applicable).
    /// </summary>
    public int? LabTestId { get; set; }

    /// <summary>
    /// Timestamp when notification was created/queued.
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp when notification was successfully delivered.
    /// </summary>
    public DateTime? DeliveredAt { get; set; }

    /// <summary>
    /// Timestamp of last retry attempt.
    /// </summary>
    public DateTime? LastRetryAt { get; set; }

    /// <summary>
    /// System user who triggered the notification.
    /// </summary>
    [StringLength(450)]
    public string? TriggeredBy { get; set; }

    // Navigation properties
    [ForeignKey(nameof(PatientId))]
    public virtual Patient? Patient { get; set; }

    [ForeignKey(nameof(AppointmentId))]
    public virtual Appointment? Appointment { get; set; }

    [ForeignKey(nameof(ProcedureRequestId))]
    public virtual ProcedureRequest? ProcedureRequest { get; set; }

    [ForeignKey(nameof(TransactionId))]
    public virtual Transaction? Transaction { get; set; }

    [ForeignKey(nameof(LabTestId))]
    public virtual LabTest? LabTest { get; set; }
}
