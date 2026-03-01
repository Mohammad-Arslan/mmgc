using System;

namespace MMGC.Shared.Exceptions;

/// <summary>
/// Base exception for the MMGC system.
/// </summary>
public class MmgcException : Exception
{
    public MmgcException(string message) : base(message) { }
    public MmgcException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Thrown when a requested resource is not found.
/// </summary>
public class EntityNotFoundException : MmgcException
{
    public EntityNotFoundException(string entityName, int id) 
        : base($"{entityName} with ID {id} not found.") { }

    public EntityNotFoundException(string entityName, string identifier)
        : base($"{entityName} with identifier '{identifier}' not found.") { }
}

/// <summary>
/// Thrown when an operation violates business rules.
/// </summary>
public class BusinessRuleViolationException : MmgcException
{
    public BusinessRuleViolationException(string message) : base(message) { }
}

/// <summary>
/// Thrown when an appointment slot is unavailable for booking.
/// </summary>
public class SlotUnavailableException : BusinessRuleViolationException
{
    public SlotUnavailableException(DateTime slotTime)
        : base($"Appointment slot at {slotTime:g} is not available.") { }
}

/// <summary>
/// Thrown when a double-booking conflict is detected.
/// </summary>
public class DoubleBookingException : BusinessRuleViolationException
{
    public DoubleBookingException(int doctorId, DateTime startTime, DateTime endTime)
        : base($"Doctor {doctorId} already has an appointment between {startTime:g} and {endTime:g}.") { }
}

/// <summary>
/// Thrown when PDF generation fails.
/// </summary>
public class PdfGenerationException : MmgcException
{
    public PdfGenerationException(string documentType, string? message = null)
        : base($"Failed to generate {documentType} PDF." + (message != null ? $" {message}" : "")) { }
}

/// <summary>
/// Thrown when notification delivery fails.
/// </summary>
public class NotificationFailedException : MmgcException
{
    public NotificationFailedException(string recipientContact, string notificationType)
        : base($"Failed to send {notificationType} to {recipientContact}.") { }
}

/// <summary>
/// Thrown when procedure approval workflow is violated.
/// </summary>
public class InvalidProcedureStateTransitionException : BusinessRuleViolationException
{
    public InvalidProcedureStateTransitionException(string currentStatus, string attemptedStatus)
        : base($"Cannot transition from status '{currentStatus}' to '{attemptedStatus}'.") { }
}

/// <summary>
/// Thrown when authorization fails for a specific action.
/// </summary>
public class UnauthorizedException : MmgcException
{
    public UnauthorizedException(string message) : base(message) { }
    public UnauthorizedException() : base("You are not authorized to perform this action.") { }
}

/// <summary>
/// Thrown when external service (SMS, Email, etc.) fails.
/// </summary>
public class ExternalServiceException : MmgcException
{
    public ExternalServiceException(string serviceName, string message, Exception? innerException = null)
        : base($"External service '{serviceName}' error: {message}", innerException) { }
}
