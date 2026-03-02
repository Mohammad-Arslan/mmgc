using MMGC.Shared.DTOs;

namespace MMGC.Shared.Interfaces;

/// <summary>
/// Service contract for managing appointment availability and slot management.
/// Enforces business rules to prevent double-booking and conflicts.
/// </summary>
public interface IAvailabilityService
{
    /// <summary>
    /// Retrieves all appointment slots for a doctor on a date with status (Available, Booked, Past).
    /// Use for display; only Available slots can be booked.
    /// </summary>
    Task<List<AppointmentSlotDto>> GetSlotsWithStatusAsync(int doctorId, DateTime date, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves available (bookable) appointment slots for a specific doctor on a given date.
    /// Excludes Booked and Past slots.
    /// </summary>
    /// <param name="doctorId">The doctor's ID.</param>
    /// <param name="date">The date to check availability.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of available appointment slots.</returns>
    Task<List<AppointmentSlotDto>> GetAvailableSlotsAsync(int doctorId, DateTime date, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all slots with status within a date range.
    /// </summary>
    Task<List<AppointmentSlotDto>> GetSlotsWithStatusInRangeAsync(int doctorId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves available (bookable) slots within a date range.
    /// </summary>
    Task<List<AppointmentSlotDto>> GetAvailableSlotsInRangeAsync(int doctorId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a specific time slot is available for booking.
    /// </summary>
    /// <param name="doctorId">The doctor's ID.</param>
    /// <param name="startTime">Slot start time.</param>
    /// <param name="endTime">Slot end time.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if slot is available, false otherwise.</returns>
    Task<bool> IsSlotAvailableAsync(int doctorId, DateTime startTime, DateTime endTime, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reserves a specific appointment slot.
    /// Uses database transaction to prevent race conditions.
    /// </summary>
    /// <param name="doctorId">The doctor's ID.</param>
    /// <param name="patientId">The patient's ID.</param>
    /// <param name="startTime">Slot start time.</param>
    /// <param name="endTime">Slot end time.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Appointment ID if successful.</returns>
    /// <exception cref="SlotUnavailableException">Thrown if slot is not available.</exception>
    /// <exception cref="DoubleBookingException">Thrown if booking would cause double-booking.</exception>
    Task<int> ReserveSlotAsync(int doctorId, int patientId, DateTime startTime, DateTime endTime, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks for conflicts in a given time range.
    /// </summary>
    Task<bool> HasConflictAsync(int doctorId, DateTime startTime, DateTime endTime, int? excludeAppointmentId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets doctor's working hours for a specific date.
    /// </summary>
    Task<(TimeSpan StartTime, TimeSpan EndTime)?> GetDoctorWorkingHoursAsync(int doctorId, DateTime date, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all booked appointments for a doctor in a date range.
    /// </summary>
    Task<List<(DateTime StartTime, DateTime EndTime)>> GetBookedSlotsAsync(int doctorId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
}
