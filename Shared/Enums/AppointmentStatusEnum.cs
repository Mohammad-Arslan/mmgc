namespace MMGC.Shared.Enums;

/// <summary>
/// Represents the status of an appointment throughout its lifecycle.
/// </summary>
public enum AppointmentStatusEnum
{
    /// <summary>
    /// Appointment is scheduled and confirmed.
    /// </summary>
    Scheduled = 0,

    /// <summary>
    /// Appointment has been completed.
    /// </summary>
    Completed = 1,

    /// <summary>
    /// Appointment has been cancelled by patient or provider.
    /// </summary>
    Cancelled = 2,

    /// <summary>
    /// Appointment is no-show (patient did not arrive).
    /// </summary>
    NoShow = 3,

    /// <summary>
    /// Appointment is rescheduled.
    /// </summary>
    Rescheduled = 4
}
