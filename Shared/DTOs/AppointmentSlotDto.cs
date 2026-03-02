using MMGC.Shared.Enums;

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
    /// Schedule ID for the slot.
    /// </summary>
    public int ScheduleId { get; set; }

    /// <summary>
    /// Date of the appointment slot.
    /// </summary>
    public DateTime ScheduleDate { get; set; }

    /// <summary>
    /// Start time of the appointment slot (as TimeSpan).
    /// </summary>
    public TimeSpan StartTime { get; set; }

    /// <summary>
    /// End time of the appointment slot (as TimeSpan).
    /// </summary>
    public TimeSpan EndTime { get; set; }

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

    /// <summary>
    /// Slot status: Available (can book), Booked (already taken), Past (time has passed).
    /// </summary>
    public SlotStatusEnum SlotStatus { get; set; } = SlotStatusEnum.Available;
}
