namespace MMGC.Shared.DTOs;

/// <summary>
/// Data transfer object for appointment slot information.
/// </summary>
public class AppointmentSlotDto
{
    /// <summary>
    /// Unique identifier for the slot.
    /// </summary>
    public string SlotId { get; set; } = string.Empty;

    /// <summary>
    /// Start time of the appointment slot.
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// End time of the appointment slot.
    /// </summary>
    public DateTime EndTime { get; set; }

    /// <summary>
    /// Indicates whether the slot is available for booking.
    /// </summary>
    public bool IsAvailable { get; set; }

    /// <summary>
    /// Doctor's ID associated with this slot.
    /// </summary>
    public int DoctorId { get; set; }

    /// <summary>
    /// Doctor's full name.
    /// </summary>
    public string DoctorName { get; set; } = string.Empty;

    /// <summary>
    /// Doctor's specialization.
    /// </summary>
    public string Specialization { get; set; } = string.Empty;
}
