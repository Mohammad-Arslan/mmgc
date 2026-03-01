namespace MMGC.Shared.Enums;

/// <summary>
/// Represents the type of notification that can be sent to patients and staff.
/// </summary>
public enum NotificationTypeEnum
{
    /// <summary>
    /// Appointment confirmation or reminder notification.
    /// </summary>
    AppointmentConfirmation = 0,

    /// <summary>
    /// Appointment cancellation notification.
    /// </summary>
    AppointmentCancellation = 1,

    /// <summary>
    /// Appointment reminder (24 hours before).
    /// </summary>
    AppointmentReminder = 2,

    /// <summary>
    /// Procedure approval notification.
    /// </summary>
    ProcedureApproved = 3,

    /// <summary>
    /// Procedure rejection notification.
    /// </summary>
    ProcedureRejected = 4,

    /// <summary>
    /// Lab report ready for viewing.
    /// </summary>
    LabReportReady = 5,

    /// <summary>
    /// Invoice/bill generated.
    /// </summary>
    InvoiceGenerated = 6,

    /// <summary>
    /// Payment received confirmation.
    /// </summary>
    PaymentReceived = 7,

    /// <summary>
    /// Prescription ready for pickup.
    /// </summary>
    PrescriptionReady = 8,

    /// <summary>
    /// Test reminder notification.
    /// </summary>
    TestReminder = 9
}
