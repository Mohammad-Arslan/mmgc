using MMGC.Shared.DTOs;

namespace MMGC.Shared.Interfaces;

/// <summary>
/// Service contract for sending notifications via various channels (SMS, Email, WhatsApp).
/// Implements strategy pattern for extensibility.
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Sends a notification message through the specified channel.
    /// Non-blocking; failures are logged and retried asynchronously.
    /// </summary>
    /// <param name="message">The notification message DTO.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Notification ID for tracking.</returns>
    Task<string> SendAsync(NotificationMessageDto message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends SMS notification to patient.
    /// </summary>
    Task<string> SendSmsAsync(string recipientPhone, string messageBody, int? patientId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends email notification to patient.
    /// </summary>
    Task<string> SendEmailAsync(string recipientEmail, string subject, string messageBody, int? patientId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends appointment confirmation notification.
    /// Auto-selects appropriate channel(s) based on patient preferences.
    /// </summary>
    Task<List<string>> SendAppointmentConfirmationAsync(int appointmentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends appointment reminder (24 hours before).
    /// </summary>
    Task<List<string>> SendAppointmentReminderAsync(int appointmentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends appointment cancellation notification.
    /// </summary>
    Task<List<string>> SendAppointmentCancellationAsync(int appointmentId, string reason, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends procedure approval notification.
    /// </summary>
    Task<List<string>> SendProcedureApprovedAsync(int procedureRequestId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends procedure rejection notification.
    /// </summary>
    Task<List<string>> SendProcedureRejectedAsync(int procedureRequestId, string reason, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends lab report ready notification.
    /// </summary>
    Task<List<string>> SendLabReportReadyAsync(int labTestId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends invoice/payment notification.
    /// </summary>
    Task<List<string>> SendInvoiceNotificationAsync(int transactionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves notification status/history.
    /// </summary>
    Task<NotificationStatusDto?> GetNotificationStatusAsync(string notificationId, CancellationToken cancellationToken = default);
}

/// <summary>
/// DTO for notification delivery status.
/// </summary>
public class NotificationStatusDto
{
    public string NotificationId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty; // Sent, Delivered, Failed, Pending
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public int RetryCount { get; set; }
}
