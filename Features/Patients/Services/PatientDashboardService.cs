using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MMGC.Data;
using MMGC.Models;
using MMGC.Shared.DTOs;
using MMGC.Shared.Exceptions;
using MMGC.Shared.Interfaces;

namespace MMGC.Features.Patients.Services;

/// <summary>
/// Service for aggregating patient dashboard data.
/// Combines data from appointments, prescriptions, lab tests, transactions, and procedures.
/// </summary>
public class PatientDashboardService : IPatientDashboardService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PatientDashboardService> _logger;

    public PatientDashboardService(
        ApplicationDbContext context,
        ILogger<PatientDashboardService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<PatientDashboardDto> GetPatientDashboardAsync(
        int patientId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var patient = await _context.Patients
                .FirstOrDefaultAsync(p => p.Id == patientId, cancellationToken);

            if (patient == null)
                throw new EntityNotFoundException(nameof(Models.Patient), patientId);

            var dashboard = new PatientDashboardDto
            {
                PatientId = patientId,
                MRNumber = patient.MRNumber,
                FullName = patient.FullName,
                DateOfBirth = patient.DateOfBirth,
                ContactNumber = patient.ContactNumber,
                Email = patient.Email,
                LastRefreshed = DateTime.UtcNow
            };

            // Get upcoming appointments count
            var upcomingAppointments = await _context.Appointments
                .AsNoTracking()
                .Where(a => a.PatientId == patientId && a.AppointmentDate > DateTime.Now)
                .OrderBy(a => a.AppointmentDate)
                .Take(5)
                .Include(a => a.Doctor)
                .Select(a => new DashboardItemDto
                {
                    Id = a.Id,
                    ItemType = "Appointment",
                    Title = $"Appointment with Dr. {a.Doctor!.FullName}",
                    Description = a.Reason,
                    Status = a.Status,
                    Date = a.AppointmentDate,
                    ProviderName = a.Doctor!.FullName,
                    IconClass = "bi-calendar-check"
                })
                .ToListAsync(cancellationToken);

            dashboard.UpcomingAppointmentsCount = await _context.Appointments
                .AsNoTracking()
                .CountAsync(a => a.PatientId == patientId && a.AppointmentDate > DateTime.Now, cancellationToken);

            dashboard.UpcomingAppointments = upcomingAppointments;

            // Get last visit date
            dashboard.LastVisitDate = await _context.Appointments
                .AsNoTracking()
                .Where(a => a.PatientId == patientId && a.AppointmentDate <= DateTime.Now)
                .OrderByDescending(a => a.AppointmentDate)
                .Select(a => a.AppointmentDate)
                .FirstOrDefaultAsync(cancellationToken);

            // Get recent prescriptions
            var recentPrescriptions = await _context.Prescriptions
                .AsNoTracking()
                .Where(p => p.PatientId == patientId)
                .Include(p => p.Doctor)
                .OrderByDescending(p => p.CreatedDate)
                .Take(5)
                .Select(p => new DashboardItemDto
                {
                    Id = p.Id,
                    ItemType = "Prescription",
                    Title = $"Prescription from Dr. {p.Doctor!.FullName}",
                    Description = p.PrescriptionDetails.Substring(0, Math.Min(50, p.PrescriptionDetails.Length)) + "...",
                    Status = "Active",
                    Date = p.PrescriptionDate,
                    ProviderName = p.Doctor!.FullName,
                    ActionUrl = $"/Reports/Prescription/{p.Id}",
                    IconClass = "bi-file-earmark-text"
                })
                .ToListAsync(cancellationToken);

            dashboard.RecentPrescriptions = recentPrescriptions;

            // Get recent lab tests
            var recentLabTests = await _context.LabTests
                .AsNoTracking()
                .Where(lt => lt.PatientId == patientId)
                .Include(lt => lt.LabTestCategory)
                .OrderByDescending(lt => lt.TestDate)
                .Take(5)
                .Select(lt => new DashboardItemDto
                {
                    Id = lt.Id,
                    ItemType = "LabTest",
                    Title = lt.TestName,
                    Status = lt.Status,
                    Date = lt.TestDate,
                    DueDate = lt.Status == "Completed" ? null : lt.TestDate.AddDays(2),
                    ActionUrl = lt.Status == "Completed" ? $"/Reports/LabTest/{lt.Id}" : null,
                    RequiresAction = lt.Status != "Completed",
                    IconClass = "bi-flask"
                })
                .ToListAsync(cancellationToken);

            dashboard.PendingLabTestsCount = recentLabTests.Count(lt => lt.Status != "Completed");
            dashboard.RecentLabTests = recentLabTests;

            // Get outstanding invoices
            var outstandingInvoices = await _context.Transactions
                .AsNoTracking()
                .Where(t => t.PatientId == patientId && t.Status == "Pending")
                .OrderByDescending(t => t.TransactionDate)
                .Take(5)
                .Select(t => new DashboardItemDto
                {
                    Id = t.Id,
                    ItemType = "Invoice",
                    Title = $"Invoice #{t.Id}",
                    Description = $"Amount: {t.Amount:C}",
                    Status = t.Status,
                    Date = t.TransactionDate,
                    ActionUrl = $"/Reports/Invoice/{t.Id}",
                    RequiresAction = true,
                    IconClass = "bi-receipt"
                })
                .ToListAsync(cancellationToken);

            dashboard.OutstandingInvoicesCount = outstandingInvoices.Count;
            dashboard.OutstandingAmount = outstandingInvoices.Sum(i => decimal.Parse(i.Description.Replace("Amount: ", "").Replace("$", "")));
            dashboard.OutstandingInvoices = outstandingInvoices;

            return dashboard;
        }
        catch (EntityNotFoundException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving dashboard for patient {PatientId}", patientId);
            throw;
        }
    }

    public async Task<(List<DashboardItemDto> Items, int TotalCount)> GetAppointmentHistoryAsync(
        int patientId,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Appointments
            .AsNoTracking()
            .Where(a => a.PatientId == patientId)
            .Include(a => a.Doctor)
            .OrderByDescending(a => a.AppointmentDate);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new DashboardItemDto
            {
                Id = a.Id,
                ItemType = "Appointment",
                Title = $"Appointment with Dr. {a.Doctor!.FullName}",
                Description = a.Reason,
                Status = a.Status,
                Date = a.AppointmentDate,
                ProviderName = a.Doctor!.FullName,
                IconClass = "bi-calendar-check"
            })
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<(List<DashboardItemDto> Items, int TotalCount)> GetPrescriptionHistoryAsync(
        int patientId,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Prescriptions
            .AsNoTracking()
            .Where(p => p.PatientId == patientId)
            .Include(p => p.Doctor)
            .OrderByDescending(p => p.CreatedDate);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new DashboardItemDto
            {
                Id = p.Id,
                ItemType = "Prescription",
                Title = $"Prescription from Dr. {p.Doctor!.FullName}",
                Description = p.PrescriptionDetails.Substring(0, Math.Min(100, p.PrescriptionDetails.Length)),
                Status = "Active",
                Date = p.CreatedDate,
                ProviderName = p.Doctor!.FullName,
                ActionUrl = $"/Reports/Prescription/{p.Id}",
                IconClass = "bi-file-earmark-text"
            })
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<(List<DashboardItemDto> Items, int TotalCount)> GetLabTestHistoryAsync(
        int patientId,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var query = _context.LabTests
            .AsNoTracking()
            .Where(lt => lt.PatientId == patientId)
            .Include(lt => lt.LabTestCategory)
            .OrderByDescending(lt => lt.TestDate);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(lt => new DashboardItemDto
            {
                Id = lt.Id,
                ItemType = "LabTest",
                Title = lt.TestName,
                Description = $"Category: {lt.LabTestCategory!.CategoryName}",
                Status = lt.Status,
                Date = lt.TestDate,
                ActionUrl = lt.Status == "Completed" ? $"/Reports/LabTest/{lt.Id}" : null,
                RequiresAction = lt.Status != "Completed",
                IconClass = "bi-flask"
            })
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<(List<DashboardItemDto> Items, int TotalCount)> GetOutstandingInvoicesAsync(
        int patientId,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Transactions
            .AsNoTracking()
            .Where(t => t.PatientId == patientId && t.Status == "Pending")
            .OrderByDescending(t => t.TransactionDate);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new DashboardItemDto
            {
                Id = t.Id,
                ItemType = "Invoice",
                Title = $"Invoice #{t.Id}",
                Description = $"Amount: {t.Amount:C}",
                Status = t.Status,
                Date = t.TransactionDate,
                ActionUrl = $"/Reports/Invoice/{t.Id}",
                RequiresAction = true,
                IconClass = "bi-receipt"
            })
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<(List<DashboardItemDto> Items, int TotalCount)> GetProcedureHistoryAsync(
        int patientId,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Procedures
            .AsNoTracking()
            .Where(p => p.PatientId == patientId)
            .Include(p => p.Doctor)
            .OrderByDescending(p => p.ProcedureDate);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new DashboardItemDto
            {
                Id = p.Id,
                ItemType = "Procedure",
                Title = p.ProcedureName,
                Description = p.TreatmentSummary,
                Status = p.Status,
                Date = p.ProcedureDate,
                ProviderName = p.Doctor!.FullName,
                ActionUrl = $"/Reports/Procedure/{p.Id}",
                IconClass = "bi-hospital"
            })
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
