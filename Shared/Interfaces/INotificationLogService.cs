using MMGC.Shared.DTOs;

namespace MMGC.Shared.Interfaces;

/// <summary>
/// Service contract for logging notification delivery.
/// Tracks all notification sends for audit and retry purposes.
/// </summary>
public interface INotificationLogService
{
    /// <summary>
    /// Creates a new notification log entry.
    /// </summary>
    /// <param name="message">The message being sent.</param>
    /// <param name="providerType">Type of provider (SMS, Email, WhatsApp).</param>
    /// <param name="triggeredBy">User ID who triggered the notification.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Created notification ID.</returns>
    Task<string> LogNotificationAsync(NotificationMessageDto message, string providerType, string? triggeredBy = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates log entry with successful delivery information.
    /// </summary>
    /// <param name="notificationId">Notification ID to update.</param>
    /// <param name="externalMessageId">ID from external service (Twilio, SendGrid, etc.).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task MarkAsDeliveredAsync(string notificationId, string? externalMessageId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates log entry with delivery failure information.
    /// </summary>
    /// <param name="notificationId">Notification ID to update.</param>
    /// <param name="errorMessage">Error description.</param>
    /// <param name="canRetry">Whether automatic retry should be attempted.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task MarkAsFailedAsync(string notificationId, string errorMessage, bool canRetry = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Increments retry count for a notification.
    /// </summary>
    /// <param name="notificationId">Notification ID to retry.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>New retry count.</returns>
    Task<int> IncrementRetryCountAsync(string notificationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a notification log entry.
    /// </summary>
    /// <param name="notificationId">Notification ID to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Notification status DTO or null if not found.</returns>
    Task<NotificationStatusDto?> GetNotificationStatusAsync(string notificationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets failed notifications that should be retried.
    /// </summary>
    /// <param name="maxRetries">Maximum retry attempts allowed.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of failed notifications eligible for retry.</returns>
    Task<List<(string NotificationId, string RecipientContact, string Message, string ProviderType)>> GetFailedNotificationsForRetryAsync(int maxRetries, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets notification history for a patient.
    /// </summary>
    /// <param name="patientId">Patient ID.</param>
    /// <param name="pageNumber">Page number.</param>
    /// <param name="pageSize">Items per page.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paginated notification history.</returns>
    Task<(List<NotificationStatusDto> Items, int TotalCount)> GetPatientNotificationHistoryAsync(int patientId, int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cleans up old notification logs (older than specified days).
    /// </summary>
    /// <param name="olderThanDays">Delete logs older than this many days.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of records deleted.</returns>
    Task<int> CleanupOldNotificationsAsync(int olderThanDays = 90, CancellationToken cancellationToken = default);
}
