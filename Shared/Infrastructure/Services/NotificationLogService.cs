using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MMGC.Data;
using MMGC.Models;
using MMGC.Shared.DTOs;
using MMGC.Shared.Enums;
using MMGC.Shared.Interfaces;

namespace MMGC.Shared.Infrastructure.Services;

/// <summary>
/// Implementation of notification logging service.
/// Persists all notification sends for audit trail and retry management.
/// </summary>
public class NotificationLogService : INotificationLogService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<NotificationLogService> _logger;

    public NotificationLogService(
        ApplicationDbContext context,
        ILogger<NotificationLogService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<string> LogNotificationAsync(
        NotificationMessageDto message,
        string providerType,
        string? triggeredBy = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var notificationId = Guid.NewGuid().ToString();

            var log = new NotificationLog
            {
                NotificationId = notificationId,
                RecipientContact = message.RecipientContact,
                NotificationType = providerType,
                NotificationTypeEnum = (NotificationTypeEnum)(message.NotificationTypeEnum ?? (int)NotificationTypeEnum.AppointmentConfirmation),
                MessageContent = message.MessageBody,
                PatientId = message.PatientId,
                AppointmentId = message.AppointmentId,
                TransactionId = message.TransactionId,
                LabTestId = message.LabTestId,
                Status = "Pending",
                TriggeredBy = triggeredBy
            };

            _context.NotificationLogs.Add(log);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Notification logged: ID={NotificationId}, Type={Type}, Contact={Contact}",
                notificationId,
                providerType,
                MaskContact(message.RecipientContact));

            return notificationId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log notification");
            throw;
        }
    }

    public async Task MarkAsDeliveredAsync(
        string notificationId,
        string? externalMessageId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var log = await _context.NotificationLogs
                .FirstOrDefaultAsync(nl => nl.NotificationId == notificationId, cancellationToken);

            if (log == null)
            {
                _logger.LogWarning("Notification not found: {NotificationId}", notificationId);
                return;
            }

            log.Status = "Delivered";
            log.DeliveredAt = DateTime.UtcNow;
            log.ExternalMessageId = externalMessageId;

            _context.NotificationLogs.Update(log);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Notification marked as delivered: ID={NotificationId}, ExternalId={ExternalId}",
                notificationId,
                externalMessageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to mark notification as delivered: {NotificationId}", notificationId);
            throw;
        }
    }

    public async Task MarkAsFailedAsync(
        string notificationId,
        string errorMessage,
        bool canRetry = true,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var log = await _context.NotificationLogs
                .FirstOrDefaultAsync(nl => nl.NotificationId == notificationId, cancellationToken);

            if (log == null)
            {
                _logger.LogWarning("Notification not found: {NotificationId}", notificationId);
                return;
            }

            log.Status = canRetry ? "FailedRetryPending" : "Failed";
            log.ErrorMessage = errorMessage;

            _context.NotificationLogs.Update(log);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogWarning(
                "Notification marked as failed: ID={NotificationId}, Error={Error}, CanRetry={CanRetry}",
                notificationId,
                errorMessage,
                canRetry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to mark notification as failed: {NotificationId}", notificationId);
            throw;
        }
    }

    public async Task<int> IncrementRetryCountAsync(
        string notificationId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var log = await _context.NotificationLogs
                .FirstOrDefaultAsync(nl => nl.NotificationId == notificationId, cancellationToken);

            if (log == null)
            {
                _logger.LogWarning("Notification not found for retry: {NotificationId}", notificationId);
                return -1;
            }

            log.RetryCount++;
            log.LastRetryAt = DateTime.UtcNow;
            log.Status = "Pending"; // Reset to pending for retry attempt

            _context.NotificationLogs.Update(log);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Retry count incremented for notification: ID={NotificationId}, RetryCount={RetryCount}",
                notificationId,
                log.RetryCount);

            return log.RetryCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to increment retry count: {NotificationId}", notificationId);
            throw;
        }
    }

    public async Task<NotificationStatusDto?> GetNotificationStatusAsync(
        string notificationId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var log = await _context.NotificationLogs
                .AsNoTracking()
                .FirstOrDefaultAsync(nl => nl.NotificationId == notificationId, cancellationToken);

            if (log == null)
                return null;

            return MapToStatusDto(log);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get notification status: {NotificationId}", notificationId);
            throw;
        }
    }

    public async Task<List<(string NotificationId, string RecipientContact, string Message, string ProviderType)>> GetFailedNotificationsForRetryAsync(
        int maxRetries,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var failedNotifications = await _context.NotificationLogs
                .AsNoTracking()
                .Where(nl => nl.Status.Contains("Failed") && nl.RetryCount < maxRetries)
                .OrderBy(nl => nl.LastRetryAt)
                .Select(nl => new
                {
                    nl.NotificationId,
                    nl.RecipientContact,
                    nl.MessageContent,
                    nl.NotificationType
                })
                .ToListAsync(cancellationToken);

            return failedNotifications
                .Select(n => (n.NotificationId, n.RecipientContact, n.MessageContent, n.NotificationType))
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve failed notifications for retry");
            throw;
        }
    }

    public async Task<(List<NotificationStatusDto> Items, int TotalCount)> GetPatientNotificationHistoryAsync(
        int patientId,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.NotificationLogs
                .AsNoTracking()
                .Where(nl => nl.PatientId == patientId)
                .OrderByDescending(nl => nl.CreatedAt);

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(nl => new NotificationStatusDto
                {
                    NotificationId = nl.NotificationId,
                    Status = nl.Status,
                    ErrorMessage = nl.ErrorMessage,
                    CreatedAt = nl.CreatedAt,
                    DeliveredAt = nl.DeliveredAt,
                    RetryCount = nl.RetryCount
                })
                .ToListAsync(cancellationToken);

            return (items, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get notification history for patient: {PatientId}", patientId);
            throw;
        }
    }

    public async Task<int> CleanupOldNotificationsAsync(
        int olderThanDays = 90,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-olderThanDays);

            var logsToDelete = await _context.NotificationLogs
                .Where(nl => nl.CreatedAt < cutoffDate && nl.Status == "Delivered")
                .ToListAsync(cancellationToken);

            if (logsToDelete.Count == 0)
            {
                _logger.LogInformation("No old notifications to clean up (older than {Days} days)", olderThanDays);
                return 0;
            }

            _context.NotificationLogs.RemoveRange(logsToDelete);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Cleaned up {Count} old notifications (older than {Days} days)",
                logsToDelete.Count,
                olderThanDays);

            return logsToDelete.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup old notifications");
            throw;
        }
    }

    private static NotificationStatusDto MapToStatusDto(NotificationLog log)
    {
        return new NotificationStatusDto
        {
            NotificationId = log.NotificationId,
            Status = log.Status,
            ErrorMessage = log.ErrorMessage,
            CreatedAt = log.CreatedAt,
            DeliveredAt = log.DeliveredAt,
            RetryCount = log.RetryCount
        };
    }

    private static string MaskContact(string contact)
    {
        if (contact.Length <= 4)
            return "****";

        var lastFour = contact.Substring(contact.Length - 4);
        return $"****{lastFour}";
    }
}
