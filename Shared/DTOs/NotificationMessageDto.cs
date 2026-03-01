namespace MMGC.Shared.DTOs;

/// <summary>
/// Data transfer object for notification message to be sent.
/// </summary>
public class NotificationMessageDto
{
    /// <summary>
    /// Recipient's phone number (for SMS) or email address (for Email).
    /// </summary>
    public string RecipientContact { get; set; } = string.Empty;

    /// <summary>
    /// Recipient's name for personalization.
    /// </summary>
    public string RecipientName { get; set; } = string.Empty;

    /// <summary>
    /// Type of notification (SMS, Email, WhatsApp).
    /// </summary>
    public string NotificationType { get; set; } = string.Empty;

    /// <summary>
    /// Message subject (for email).
    /// </summary>
    public string? Subject { get; set; }

    /// <summary>
    /// Message body/content.
    /// </summary>
    public string MessageBody { get; set; } = string.Empty;

    /// <summary>
    /// Associated patient ID (for audit trail).
    /// </summary>
    public int? PatientId { get; set; }

    /// <summary>
    /// Related appointment ID (if applicable).
    /// </summary>
    public int? AppointmentId { get; set; }

    /// <summary>
    /// Related transaction ID (if applicable).
    /// </summary>
    public int? TransactionId { get; set; }

    /// <summary>
    /// Related lab test ID (if applicable).
    /// </summary>
    public int? LabTestId { get; set; }

    /// <summary>
    /// Custom metadata for template rendering.
    /// </summary>
    public Dictionary<string, string>? TemplateData { get; set; }

    /// <summary>
    /// Priority level: Low, Normal, High.
    /// </summary>
    public int Priority { get; set; } = 1;

    /// <summary>
    /// Notification type enum value.
    /// </summary>
    public int? NotificationTypeEnum { get; set; }
}
