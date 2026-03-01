using MMGC.Models;

namespace MMGC.Services;

public interface IDoctorDashboardService
{
    Task<Doctor?> GetDoctorProfileAsync(int doctorId);
    Task<IEnumerable<Appointment>> GetAppointmentsHistoryAsync(int doctorId, DateTime? fromDate = null, DateTime? toDate = null);
    Task<IEnumerable<Patient>> GetPatientsListAsync(int doctorId);
    Task<Patient?> GetPatientWithCompleteHistoryAsync(int patientId, int doctorId);
    Task<IEnumerable<Procedure>> GetProceduresByDoctorAsync(int doctorId, DateTime? fromDate = null, DateTime? toDate = null);
    Task<Dictionary<string, object>> GetDashboardStatisticsAsync(int doctorId);
}

