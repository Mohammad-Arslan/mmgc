using MMGC.Models;
using MMGC.Data;
using Microsoft.EntityFrameworkCore;

namespace MMGC.Services;

public class DoctorDashboardService : IDoctorDashboardService
{
    private readonly ApplicationDbContext _context;

    public DoctorDashboardService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Doctor?> GetDoctorProfileAsync(int doctorId)
    {
        return await _context.Doctors
            .Include(d => d.Schedules)
            .FirstOrDefaultAsync(d => d.Id == doctorId);
    }

    public async Task<IEnumerable<Appointment>> GetAppointmentsHistoryAsync(int doctorId, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var query = _context.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Nurse)
            .Where(a => a.DoctorId == doctorId);

        if (fromDate.HasValue)
        {
            query = query.Where(a => a.AppointmentDate >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(a => a.AppointmentDate <= toDate.Value);
        }

        return await query
            .OrderByDescending(a => a.AppointmentDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<Patient>> GetPatientsListAsync(int doctorId)
    {
        // Get all unique patients who have appointments or procedures with this doctor
        var patientIds = await _context.Appointments
            .Where(a => a.DoctorId == doctorId)
            .Select(a => a.PatientId)
            .Union(
                _context.Procedures
                    .Where(p => p.DoctorId == doctorId)
                    .Select(p => p.PatientId)
            )
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToListAsync();

        return await _context.Patients
            .Where(p => patientIds.Contains(p.Id))
            .OrderBy(p => p.FirstName)
            .ThenBy(p => p.LastName)
            .ToListAsync();
    }

    public async Task<Patient?> GetPatientWithCompleteHistoryAsync(int patientId, int doctorId)
    {
        // Verify that this patient has been seen by this doctor
        var hasRelation = await _context.Appointments.AnyAsync(a => a.PatientId == patientId && a.DoctorId == doctorId)
            || await _context.Procedures.AnyAsync(p => p.PatientId == patientId && p.DoctorId == doctorId);

        if (!hasRelation)
        {
            return null;
        }

        return await _context.Patients
            .Include(p => p.Appointments.Where(a => a.DoctorId == doctorId))
                .ThenInclude(a => a.Nurse)
            .Include(p => p.Procedures.Where(pr => pr.DoctorId == doctorId))
                .ThenInclude(pr => pr.Nurse)
            .Include(p => p.Prescriptions.Where(pr => pr.DoctorId == doctorId))
            .Include(p => p.LabTests)
                .ThenInclude(lt => lt.LabTestCategory)
            .Include(p => p.Transactions)
            .FirstOrDefaultAsync(p => p.Id == patientId);
    }

    public async Task<IEnumerable<Procedure>> GetProceduresByDoctorAsync(int doctorId, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var query = _context.Procedures
            .Include(p => p.Patient)
            .Include(p => p.Nurse)
            .Include(p => p.LabTests)
            .Where(p => p.DoctorId == doctorId);

        if (fromDate.HasValue)
        {
            query = query.Where(p => p.ProcedureDate >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(p => p.ProcedureDate <= toDate.Value);
        }

        return await query
            .OrderByDescending(p => p.ProcedureDate)
            .ToListAsync();
    }

    public async Task<Dictionary<string, object>> GetDashboardStatisticsAsync(int doctorId)
    {
        var today = DateTime.Today;
        var thisMonth = new DateTime(today.Year, today.Month, 1);
        var thisYear = new DateTime(today.Year, 1, 1);

        var totalAppointments = await _context.Appointments
            .CountAsync(a => a.DoctorId == doctorId);

        var todayAppointments = await _context.Appointments
            .CountAsync(a => a.DoctorId == doctorId && a.AppointmentDate.Date == today);

        var thisMonthAppointments = await _context.Appointments
            .CountAsync(a => a.DoctorId == doctorId && a.AppointmentDate >= thisMonth);

        var totalProcedures = await _context.Procedures
            .CountAsync(p => p.DoctorId == doctorId);

        var totalPatients = await GetPatientsListAsync(doctorId)
            .ContinueWith(t => t.Result.Count());

        var monthlyRevenue = await _context.Appointments
            .Where(a => a.DoctorId == doctorId && a.AppointmentDate >= thisMonth && a.Status == "Completed")
            .SumAsync(a => (decimal?)a.ConsultationFee) ?? 0;

        monthlyRevenue += await _context.Procedures
            .Where(p => p.DoctorId == doctorId && p.ProcedureDate >= thisMonth && p.Status == "Completed")
            .SumAsync(p => (decimal?)p.ProcedureFee) ?? 0;

        return new Dictionary<string, object>
        {
            { "TotalAppointments", totalAppointments },
            { "TodayAppointments", todayAppointments },
            { "ThisMonthAppointments", thisMonthAppointments },
            { "TotalProcedures", totalProcedures },
            { "TotalPatients", totalPatients },
            { "MonthlyRevenue", monthlyRevenue }
        };
    }
}

