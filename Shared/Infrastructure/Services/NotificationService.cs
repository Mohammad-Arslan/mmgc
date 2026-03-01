using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MMGC.Data;
using MMGC.Models;
using MMGC.Shared.Constants;
using MMGC.Shared.DTOs;
using MMGC.Shared.Enums;
using MMGC.Shared.Exceptions;
using MMGC.Shared.Interfaces;

namespace MMGC.Shared.Infrastructure.Services;

/// <summary>
/// Core notification service that coordinates multi-channel notification delivery.
/// Uses notification providers (SMS, Email, WhatsApp) based on patient preferences.
/// Fire-and-forget pattern: exceptions don't propagate to caller.
/// </summary>
public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _context;
    private readonly INotificationLogService _logService;
    private readonly ILogger<NotificationService> _logger;
    private readonly Dictionary<string, INotificationProvider> _providers;

    public NotificationService(
        ApplicationDbContext context,
        INotificationLogService logService,
        ILogger<NotificationService> logger,
        IEnumerable<INotificationProvider> providers)
    {
        _context = context;
        _logService = logService;
        _logger = logger;
        _providers = providers
            .Where(p => p.IsEnabled())
            .ToDictionary(p => p.ProviderType, p => p);
    }

    public async Task<string> SendAsync(
        NotificationMessageDto message,
        CancellationToken cancellationToken = default)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        var notificationId = Guid.NewGuid().ToString();

        try
        {
            // Log the notification
            notificationId = await _logService.LogNotificationAsync(
                message,
                message.NotificationType ?? "SMS",
                cancellationToken: cancellationToken);

            // Send asynchronously without blocking
            _ = SendNotificationBackgroundAsync(notificationId, message);

            return notificationId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to queue notification");
            // Don't throw; mark as failed and return ID for tracking
            await _logService.MarkAsFailedAsync(notificationId, ex.Message, canRetry: true);
            return notificationId;
        }
    }

    public async Task<string> SendSmsAsync(
        string recipientPhone,
        string messageBody,
        int? patientId = null,
        CancellationToken cancellationToken = default)
    {
        var message = new NotificationMessageDto
        {
            RecipientContact = recipientPhone,
            NotificationType = "SMS",
            NotificationTypeEnum = (int)NotificationTypeEnum.AppointmentConfirmation,
            MessageBody = messageBody,
            PatientId = patientId
        };

        return await SendAsync(message, cancellationToken);
    }

    public async Task<string> SendEmailAsync(
        string recipientEmail,
        string subject,
        string messageBody,
        int? patientId = null,
        CancellationToken cancellationToken = default)
    {
        var message = new NotificationMessageDto
        {
            RecipientContact = recipientEmail,
            NotificationType = "Email",
            NotificationTypeEnum = (int)NotificationTypeEnum.AppointmentConfirmation,
            Subject = subject,
            MessageBody = messageBody,
            PatientId = patientId
        };

        return await SendAsync(message, cancellationToken);
    }

    public async Task<List<string>> SendAppointmentConfirmationAsync(
        int appointmentId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var appointment = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .FirstOrDefaultAsync(a => a.Id == appointmentId, cancellationToken);

            if (appointment?.Patient == null)
                throw new EntityNotFoundException(nameof(Appointment), appointmentId);

            var notificationIds = new List<string>();

            var message = new NotificationMessageDto
            {
                RecipientContact = appointment.Patient.ContactNumber ?? appointment.Patient.Email ?? string.Empty,
                RecipientName = appointment.Patient.FullName,
                NotificationType = "SMS",
                NotificationTypeEnum = (int)NotificationTypeEnum.AppointmentConfirmation,
                MessageBody = BuildAppointmentConfirmationMessage(appointment),
                PatientId = appointment.PatientId,
                AppointmentId = appointmentId,
                Subject = $"Appointment Confirmation - {appointment.AppointmentDate:g}"
            };

            if (!string.IsNullOrWhiteSpace(message.RecipientContact))
            {
                var id = await SendAsync(message, cancellationToken);
                notificationIds.Add(id);
            }

            return notificationIds;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send appointment confirmation for appointment {AppointmentId}", appointmentId);
            return new List<string>();
        }
    }

    public async Task<List<string>> SendAppointmentReminderAsync(
        int appointmentId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var appointment = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .FirstOrDefaultAsync(a => a.Id == appointmentId, cancellationToken);

            if (appointment?.Patient == null)
                throw new EntityNotFoundException(nameof(Appointment), appointmentId);

            // Only send reminder if appointment is within 24 hours
            var hoursUntilAppointment = (appointment.AppointmentDate - DateTime.Now).TotalHours;
            if (hoursUntilAppointment <= 0 || hoursUntilAppointment > 25)
            {
                _logger.LogInformation("Appointment reminder not applicable for appointment {AppointmentId}", appointmentId);
                return new List<string>();
            }

            var notificationIds = new List<string>();

            var message = new NotificationMessageDto
            {
                RecipientContact = appointment.Patient.ContactNumber ?? appointment.Patient.Email ?? string.Empty,
                RecipientName = appointment.Patient.FullName,
                NotificationType = "SMS",
                NotificationTypeEnum = (int)NotificationTypeEnum.AppointmentReminder,
                MessageBody = BuildAppointmentReminderMessage(appointment),
                PatientId = appointment.PatientId,
                AppointmentId = appointmentId
            };

            if (!string.IsNullOrWhiteSpace(message.RecipientContact))
            {
                var id = await SendAsync(message, cancellationToken);
                notificationIds.Add(id);
            }

            return notificationIds;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send appointment reminder for appointment {AppointmentId}", appointmentId);
            return new List<string>();
        }
    }

    public async Task<List<string>> SendAppointmentCancellationAsync(
        int appointmentId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var appointment = await _context.Appointments
                .Include(a => a.Patient)
                .FirstOrDefaultAsync(a => a.Id == appointmentId, cancellationToken);

            if (appointment?.Patient == null)
                throw new EntityNotFoundException(nameof(Appointment), appointmentId);

            var notificationIds = new List<string>();

            var message = new NotificationMessageDto
            {
                RecipientContact = appointment.Patient.ContactNumber ?? appointment.Patient.Email ?? string.Empty,
                RecipientName = appointment.Patient.FullName,
                NotificationType = "SMS",
                NotificationTypeEnum = (int)NotificationTypeEnum.AppointmentCancellation,
                MessageBody = BuildAppointmentCancellationMessage(appointment, reason),
                PatientId = appointment.PatientId,
                AppointmentId = appointmentId
            };

            if (!string.IsNullOrWhiteSpace(message.RecipientContact))
            {
                var id = await SendAsync(message, cancellationToken);
                notificationIds.Add(id);
            }

            return notificationIds;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send appointment cancellation for appointment {AppointmentId}", appointmentId);
            return new List<string>();
        }
    }

    public async Task<List<string>> SendProcedureApprovedAsync(
        int procedureRequestId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var procedureRequest = await _context.ProcedureRequests
                .Include(pr => pr.Patient)
                .FirstOrDefaultAsync(pr => pr.Id == procedureRequestId, cancellationToken);

            if (procedureRequest?.Patient == null)
                throw new EntityNotFoundException("ProcedureRequest", procedureRequestId);

            var notificationIds = new List<string>();

            var message = new NotificationMessageDto
            {
                RecipientContact = procedureRequest.Patient.ContactNumber ?? procedureRequest.Patient.Email ?? string.Empty,
                RecipientName = procedureRequest.Patient.FullName,
                NotificationType = "SMS",
                NotificationTypeEnum = (int)NotificationTypeEnum.ProcedureApproved,
                MessageBody = BuildProcedureApprovedMessage(procedureRequest),
                PatientId = procedureRequest.PatientId
            };

            if (!string.IsNullOrWhiteSpace(message.RecipientContact))
            {
                var id = await SendAsync(message, cancellationToken);
                notificationIds.Add(id);
            }

            return notificationIds;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send procedure approval notification for request {RequestId}", procedureRequestId);
            return new List<string>();
        }
    }

    public async Task<List<string>> SendProcedureRejectedAsync(
        int procedureRequestId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var procedureRequest = await _context.ProcedureRequests
                .Include(pr => pr.Patient)
                .FirstOrDefaultAsync(pr => pr.Id == procedureRequestId, cancellationToken);

            if (procedureRequest?.Patient == null)
                throw new EntityNotFoundException("ProcedureRequest", procedureRequestId);

            var notificationIds = new List<string>();

            var message = new NotificationMessageDto
            {
                RecipientContact = procedureRequest.Patient.ContactNumber ?? procedureRequest.Patient.Email ?? string.Empty,
                RecipientName = procedureRequest.Patient.FullName,
                NotificationType = "SMS",
                NotificationTypeEnum = (int)NotificationTypeEnum.ProcedureRejected,
                MessageBody = BuildProcedureRejectedMessage(procedureRequest, reason),
                PatientId = procedureRequest.PatientId
            };

            if (!string.IsNullOrWhiteSpace(message.RecipientContact))
            {
                var id = await SendAsync(message, cancellationToken);
                notificationIds.Add(id);
            }

            return notificationIds;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send procedure rejection notification for request {RequestId}", procedureRequestId);
            return new List<string>();
        }
    }

    public async Task<List<string>> SendLabReportReadyAsync(
        int labTestId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var labTest = await _context.LabTests
                .Include(lt => lt.Patient)
                .FirstOrDefaultAsync(lt => lt.Id == labTestId, cancellationToken);

            if (labTest?.Patient == null)
                throw new EntityNotFoundException(nameof(LabTest), labTestId);

            var notificationIds = new List<string>();

            var message = new NotificationMessageDto
            {
                RecipientContact = labTest.Patient.ContactNumber ?? labTest.Patient.Email ?? string.Empty,
                RecipientName = labTest.Patient.FullName,
                NotificationType = "SMS",
                NotificationTypeEnum = (int)NotificationTypeEnum.LabReportReady,
                MessageBody = BuildLabReportReadyMessage(labTest),
                PatientId = labTest.PatientId,
                LabTestId = labTestId
            };

            if (!string.IsNullOrWhiteSpace(message.RecipientContact))
            {
                var id = await SendAsync(message, cancellationToken);
                notificationIds.Add(id);
            }

            return notificationIds;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send lab report ready notification for test {TestId}", labTestId);
            return new List<string>();
        }
    }

    public async Task<List<string>> SendInvoiceNotificationAsync(
        int transactionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var transaction = await _context.Transactions
                .Include(t => t.Patient)
                .FirstOrDefaultAsync(t => t.Id == transactionId, cancellationToken);

            if (transaction?.Patient == null)
                throw new EntityNotFoundException(nameof(Transaction), transactionId);

            var notificationIds = new List<string>();

            var message = new NotificationMessageDto
            {
                RecipientContact = transaction.Patient.ContactNumber ?? transaction.Patient.Email ?? string.Empty,
                RecipientName = transaction.Patient.FullName,
                NotificationType = "SMS",
                NotificationTypeEnum = (int)NotificationTypeEnum.InvoiceGenerated,
                MessageBody = BuildInvoiceNotificationMessage(transaction),
                PatientId = transaction.PatientId,
                TransactionId = transactionId
            };

            if (!string.IsNullOrWhiteSpace(message.RecipientContact))
            {
                var id = await SendAsync(message, cancellationToken);
                notificationIds.Add(id);
            }

            return notificationIds;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send invoice notification for transaction {TransactionId}", transactionId);
            return new List<string>();
        }
    }

    public async Task<NotificationStatusDto?> GetNotificationStatusAsync(
        string notificationId,
        CancellationToken cancellationToken = default)
    {
        return await _logService.GetNotificationStatusAsync(notificationId, cancellationToken);
    }

    // Private helper methods for building message content
    private static string BuildAppointmentConfirmationMessage(Appointment appointment)
    {
        var doctorName = appointment.Doctor?.FullName ?? "Doctor";
        return $"Hello {appointment.Patient?.FirstName},\n\n" +
               $"Your appointment with Dr. {doctorName} is confirmed for {appointment.AppointmentDate:MMMM dd, yyyy at HH:mm}.\n\n" +
               $"Please arrive 10 minutes early. Contact us if you need to reschedule.\n\n" +
               "MMGC Hospital";
    }

    private static string BuildAppointmentReminderMessage(Appointment appointment)
    {
        var hoursUntil = Math.Round((appointment.AppointmentDate - DateTime.Now).TotalHours);
        return $"Reminder: Your appointment with MMGC Hospital is in {hoursUntil} hours at {appointment.AppointmentDate:HH:mm}. Please arrive on time.";
    }

    private static string BuildAppointmentCancellationMessage(Appointment appointment, string reason)
    {
        return $"Your appointment scheduled for {appointment.AppointmentDate:MMMM dd, yyyy} has been cancelled.\n\n" +
               $"Reason: {reason}\n\n" +
               "Please contact us to reschedule.";
    }

    private static string BuildProcedureApprovedMessage(ProcedureRequest request)
    {
        return $"Good news! Your request for {request.ProcedureType} has been approved by your doctor.\n\n" +
               "Please contact MMGC Hospital to schedule your procedure.\n\n" +
               "Thank you for choosing MMGC Hospital.";
    }

    private static string BuildProcedureRejectedMessage(ProcedureRequest request, string reason)
    {
        return $"Your request for {request.ProcedureType} has been reviewed.\n\n" +
               $"Reason: {reason}\n\n" +
               "Please contact your doctor to discuss alternative options.";
    }

    private static string BuildLabReportReadyMessage(LabTest labTest)
    {
        return $"Your lab test ({labTest.TestName}) results are now available.\n\n" +
               "Please log in to your patient portal to view your report.\n\n" +
               "MMGC Hospital";
    }

    private static string BuildInvoiceNotificationMessage(Transaction transaction)
    {
        return $"Invoice #{transaction.Id} of {transaction.Amount:C} has been generated.\n\n" +
               "Log in to download your invoice from the patient portal.";
    }

    private async Task SendNotificationBackgroundAsync(string notificationId, NotificationMessageDto message)
    {
        try
        {
            var provider = _providers.Values.FirstOrDefault(p => p.ValidateContact(message.RecipientContact));
            if (provider == null)
            {
                await _logService.MarkAsFailedAsync(notificationId, "No valid provider for contact", canRetry: false);
                return;
            }

            var externalId = await provider.SendAsync(
                message.RecipientContact,
                message.MessageBody,
                message.Subject);

            await _logService.MarkAsDeliveredAsync(notificationId, externalId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Background notification send failed for notification {NotificationId}", notificationId);
            await _logService.MarkAsFailedAsync(notificationId, ex.Message, canRetry: true);
        }
    }
}
