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

    public async Task<List<AppointmentSlotDto>> GetSlotsWithStatusAsync(int doctorId, DateTime date, CancellationToken cancellationToken = default)
    {
        try
        {
            var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.Id == doctorId, cancellationToken);
            if (doctor == null)
                throw new EntityNotFoundException(nameof(Models.Doctor), doctorId);

            var dateOnly = date.Date;
            var dateStart = dateOnly;
            var dateEnd = dateOnly.AddDays(1);
            var now = DateTime.Now;

            // Get booked appointments for the date
            var bookedSlots = await _context.Appointments
                .AsNoTracking()
                .Where(a => a.DoctorId == doctorId && a.AppointmentDate >= dateStart && a.AppointmentDate < dateEnd && a.StatusEnum != AppointmentStatusEnum.Cancelled)
                .OrderBy(a => a.AppointmentDate)
                .Select(a => new { a.AppointmentDate, a.AppointmentEndTime })
                .ToListAsync(cancellationToken);

            // Get doctor-defined schedules for this date (date-specific)
            var scheduleDateEnd = dateOnly.AddDays(1);
            var schedules = await _context.DoctorSchedules
                .AsNoTracking()
                .Where(ds => ds.DoctorId == doctorId && ds.ScheduleDate >= dateOnly && ds.ScheduleDate < scheduleDateEnd && ds.IsAvailable)
                .OrderBy(ds => ds.StartTime)
                .ToListAsync(cancellationToken);

            // Build time blocks ONLY from DoctorSchedules - no fallback
            var blocks = new List<(TimeSpan Start, TimeSpan End)>();
            foreach (var s in schedules)
            {
                if (s.EndTime > s.StartTime)
                    blocks.Add((s.StartTime, s.EndTime));
            }

            var slotDuration = SystemConstants.APPOINTMENT_SLOT_DURATION_MINUTES;
            var slotSet = new HashSet<TimeSpan>(); // Deduplicate by start time
            var slots = new List<AppointmentSlotDto>();

            foreach (var (blockStart, blockEnd) in blocks)
            {
                var current = dateOnly.Add(blockStart);
                var end = dateOnly.Add(blockEnd);

                while (current.AddMinutes(slotDuration) <= end)
                {
                    var slotEnd = current.AddMinutes(slotDuration);
                    if (slotSet.Add(current.TimeOfDay))
                    {
                        var isBooked = bookedSlots.Any(bs =>
                        {
                            var bsEnd = bs.AppointmentEndTime ?? bs.AppointmentDate.AddMinutes(slotDuration);
                            return !(slotEnd <= bs.AppointmentDate || current >= bsEnd);
                        });

                        SlotStatusEnum status;
                        if (dateOnly < now.Date)
                            status = SlotStatusEnum.Past;
                        else if (dateOnly == now.Date && slotEnd <= now)
                            status = SlotStatusEnum.Past;
                        else if (isBooked)
                            status = SlotStatusEnum.Booked;
                        else
                            status = SlotStatusEnum.Available;

                        slots.Add(new AppointmentSlotDto
                        {
                            SlotId = $"{doctorId}_{current:yyyyMMddHHmm}",
                            ScheduleId = 0,
                            ScheduleDate = dateOnly,
                            StartTime = current.TimeOfDay,
                            EndTime = slotEnd.TimeOfDay,
                            IsAvailable = status == SlotStatusEnum.Available,
                            DoctorId = doctorId,
                            DoctorName = doctor.FullName,
                            Specialization = doctor.Specialization,
                            SlotStatus = status
                        });
                    }
                    current = current.AddMinutes(slotDuration);
                }
            }

            return slots.OrderBy(s => s.StartTime).ToList();
        }
        catch (EntityNotFoundException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving slots for doctor {DoctorId}", doctorId);
            throw;
        }
    }

    public async Task<List<AppointmentSlotDto>> GetAvailableSlotsAsync(int doctorId, DateTime date, CancellationToken cancellationToken = default)
    {
        var allSlots = await GetSlotsWithStatusAsync(doctorId, date, cancellationToken);
        return allSlots.Where(s => s.SlotStatus == SlotStatusEnum.Available).ToList();
    }

    public async Task<List<AppointmentSlotDto>> GetSlotsWithStatusInRangeAsync(int doctorId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        var allSlots = new List<AppointmentSlotDto>();
        var currentDate = startDate.Date;

        while (currentDate <= endDate.Date)
        {
            var slotsForDay = await GetSlotsWithStatusAsync(doctorId, currentDate, cancellationToken);
            allSlots.AddRange(slotsForDay);
            currentDate = currentDate.AddDays(1);
        }

        return allSlots;
    }

    public async Task<List<AppointmentSlotDto>> GetAvailableSlotsInRangeAsync(int doctorId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        var allSlots = await GetSlotsWithStatusInRangeAsync(doctorId, startDate, endDate, cancellationToken);
        return allSlots.Where(s => s.SlotStatus == SlotStatusEnum.Available).ToList();
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
            var dateOnly = date.Date;
            var scheduleDateEnd = dateOnly.AddDays(1);
            var schedule = await _context.DoctorSchedules
                .AsNoTracking()
                .Where(ds => ds.DoctorId == doctorId && ds.ScheduleDate >= dateOnly && ds.ScheduleDate < scheduleDateEnd && ds.IsAvailable)
                .OrderBy(ds => ds.StartTime)
                .FirstOrDefaultAsync(cancellationToken);

            if (schedule != null)
                return (schedule.StartTime, schedule.EndTime);

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
