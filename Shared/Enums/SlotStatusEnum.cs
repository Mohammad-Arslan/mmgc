namespace MMGC.Shared.Enums;

/// <summary>
/// Status of an appointment slot for display and booking logic.
/// </summary>
public enum SlotStatusEnum
{
    /// <summary>Slot can be booked.</summary>
    Available = 0,

    /// <summary>Slot is already booked.</summary>
    Booked = 1,

    /// <summary>Slot time has passed; cannot be booked.</summary>
    Past = 2
}
