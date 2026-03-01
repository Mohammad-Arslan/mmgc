using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MMGC.Data;
using MMGC.Models;
using MMGC.Shared.Constants;
using MMGC.Shared.DTOs;
using MMGC.Shared.Enums;
using MMGC.Shared.Exceptions;
using MMGC.Shared.Interfaces;

namespace MMGC.Features.Appointments.Services;

public class AvailabilityService : IAvailabilityService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AvailabilityService> _logger;

    public AvailabilityService(ApplicationDbContext context, ILogger<AvailabilityService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<AppointmentSlotDto>> GetAvailableSlotsAsync(int doctorId, DateTime date, CancellationToken cancellationToken = default)
    {
        try
        {
            var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.Id == doctorId, cancellationToken);
            if (doctor == null)
                throw new EntityNotFoundException(nameof(Models.Doctor), doctorId);

            var dateStart = date.Date;
            var dateEnd = date.Date.AddDays(1);

            var bookedSlots = await _context.Appointments
                .AsNoTracking()
                .Where(a => a.DoctorId == doctorId && a.AppointmentDate >= dateStart && a.AppointmentDate < dateEnd && a.StatusEnum != AppointmentStatusEnum.Cancelled)
                .OrderBy(a => a.AppointmentDate)
                .Select(a => new { a.AppointmentDate, a.AppointmentEndTime })
                .ToListAsync(cancellationToken);

            var slots = new List<AppointmentSlotDto>();
            var slotDuration = SystemConstants.APPOINTMENT_SLOT_DURATION_MINUTES;
            var workStart = date.Date.AddHours(9);
            var workEnd = date.Date.AddHours(17);
            var currentTime = workStart;

            while (currentTime.AddMinutes(slotDuration) <= workEnd)
            {
                var slotEnd = currentTime.AddMinutes(slotDuration);
                var hasConflict = bookedSlots.Any(bs =>
                {
                    var bsEnd = bs.AppointmentEndTime ?? bs.AppointmentDate.AddMinutes(slotDuration);
                    return !(slotEnd <= bs.AppointmentDate || currentTime >= bsEnd);
                });

                if (!hasConflict)
                {
                    slots.Add(new AppointmentSlotDto
                    {
                        SlotId = $"{doctorId}_{currentTime:yyyyMMddHHmm}",
                        StartTime = currentTime,
                        EndTime = slotEnd,
                        IsAvailable = true,
                        DoctorId = doctorId,
                        DoctorName = doctor.FullName,
                        Specialization = doctor.Specialization
                    });
                }

                currentTime = currentTime.AddMinutes(slotDuration);
            }

            return slots;
        }
        catch (EntityNotFoundException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving available slots for doctor {DoctorId}", doctorId);
            throw;
        }
    }

    public async Task<List<AppointmentSlotDto>> GetAvailableSlotsInRangeAsync(int doctorId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        var allSlots = new List<AppointmentSlotDto>();
        var currentDate = startDate.Date;

        while (currentDate <= endDate.Date)
        {
            var slotsForDay = await GetAvailableSlotsAsync(doctorId, currentDate, cancellationToken);
            allSlots.AddRange(slotsForDay);
            currentDate = currentDate.AddDays(1);
        }

        return allSlots;
    }

    public async Task<bool> IsSlotAvailableAsync(int doctorId, DateTime startTime, DateTime endTime, CancellationToken cancellationToken = default)
    {
        try
        {
            if (endTime <= startTime)
                throw new ArgumentException("End time must be after start time");

            var hasConflict = await _context.Appointments
                .AsNoTracking()
                .AnyAsync(a =>
                    a.DoctorId == doctorId &&
                    a.StatusEnum != AppointmentStatusEnum.Cancelled &&
                    !(((a.AppointmentEndTime ?? a.AppointmentDate.AddMinutes(SystemConstants.APPOINTMENT_SLOT_DURATION_MINUTES)) <= startTime)) &&
                    !(a.AppointmentDate >= endTime),
                    cancellationToken);

            return !hasConflict;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking slot availability");
            throw;
        }
    }

    public async Task<int> ReserveSlotAsync(int doctorId, int patientId, DateTime startTime, DateTime endTime, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.Id == doctorId, cancellationToken);
            if (doctor == null)
                throw new EntityNotFoundException(nameof(Models.Doctor), doctorId);

            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.Id == patientId, cancellationToken);
            if (patient == null)
                throw new EntityNotFoundException(nameof(Models.Patient), patientId);

            var hasConflict = await HasConflictAsync(doctorId, startTime, endTime, cancellationToken: cancellationToken);
            if (hasConflict)
                throw new DoubleBookingException(doctorId, startTime, endTime);

            var appointment = new Appointment
            {
                PatientId = patientId,
                DoctorId = doctorId,
                AppointmentDate = startTime,
                AppointmentEndTime = endTime,
                Status = AppointmentStatusEnum.Scheduled.ToString(),
                StatusEnum = AppointmentStatusEnum.Scheduled,
                AppointmentType = "General",
                CreatedDate = DateTime.Now,
                CreatedBy = "System"
            };

            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation("Appointment reserved: Doctor={DoctorId}, Patient={PatientId}, StartTime={StartTime}", doctorId, patientId, startTime);
            return appointment.Id;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogWarning(ex, "Concurrency conflict when reserving slot");
            throw new DoubleBookingException(doctorId, startTime, endTime);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Error reserving slot");
            throw;
        }
    }

    public async Task<bool> HasConflictAsync(int doctorId, DateTime startTime, DateTime endTime, int? excludeAppointmentId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.Appointments
                .AsNoTracking()
                .Where(a =>
                    a.DoctorId == doctorId &&
                    a.StatusEnum != AppointmentStatusEnum.Cancelled &&
                    !(((a.AppointmentEndTime ?? a.AppointmentDate.AddMinutes(SystemConstants.APPOINTMENT_SLOT_DURATION_MINUTES)) <= startTime)) &&
                    !(a.AppointmentDate >= endTime));

            if (excludeAppointmentId.HasValue)
                query = query.Where(a => a.Id != excludeAppointmentId.Value);

            return await query.AnyAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking for conflicts");
            throw;
        }
    }

    public async Task<(TimeSpan StartTime, TimeSpan EndTime)?> GetDoctorWorkingHoursAsync(int doctorId, DateTime date, CancellationToken cancellationToken = default)
    {
        try
        {
            // Default: Monday-Friday 9 AM to 5 PM
            if (date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday)
                return (new TimeSpan(9, 0, 0), new TimeSpan(17, 0, 0));

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving doctor working hours");
            throw;
        }
    }

    public async Task<List<(DateTime StartTime, DateTime EndTime)>> GetBookedSlotsAsync(int doctorId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        try
        {
            var bookedSlots = await _context.Appointments
                .AsNoTracking()
                .Where(a =>
                    a.DoctorId == doctorId &&
                    a.AppointmentDate >= startDate &&
                    a.AppointmentDate <= endDate &&
                    a.StatusEnum != AppointmentStatusEnum.Cancelled)
                .Select(a => new { a.AppointmentDate, EndTime = a.AppointmentEndTime ?? a.AppointmentDate.AddMinutes(SystemConstants.APPOINTMENT_SLOT_DURATION_MINUTES) })
                .ToListAsync(cancellationToken);

            return bookedSlots.Select(bs => (bs.AppointmentDate, bs.EndTime)).OrderBy(bs => bs.AppointmentDate).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving booked slots");
            throw;
        }
    }
}
