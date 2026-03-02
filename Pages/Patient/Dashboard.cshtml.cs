using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MMGC.Data;
using MMGC.Shared.DTOs;
using MMGC.Shared.Interfaces;
using System.Security.Claims;

namespace MMGC.Pages.Patient;

/// <summary>
/// PageModel for the patient dashboard page.
/// Aggregates patient information including appointments, prescriptions, lab tests, and invoices.
/// </summary>
[Authorize(Roles = "Patient")]
public class DashboardModel : PageModel
{
    private readonly IPatientDashboardService _dashboardService;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DashboardModel> _logger;

    public PatientDashboardDto? Dashboard { get; set; }
    public List<DashboardItemDto> UpcomingAppointments { get; set; } = new();
    public List<DashboardItemDto> RecentPrescriptions { get; set; } = new();
    public List<DashboardItemDto> PendingLabTests { get; set; } = new();
    public List<DashboardItemDto> OutstandingInvoices { get; set; } = new();

    public int CurrentPatientId { get; private set; }
    public string? ErrorMessage { get; set; }
    public bool IsLoading { get; set; } = true;

    public DashboardModel(
        IPatientDashboardService dashboardService,
        ApplicationDbContext context,
        ILogger<DashboardModel> logger)
    {
        _dashboardService = dashboardService;
        _context = context;
        _logger = logger;
    }

    private async Task<int?> ResolvePatientIdAsync()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim)) return null;

        var patient = await _context.Patients
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userIdClaim);
        if (patient != null) return patient.Id;

        var queryId = HttpContext.Request.Query["patientId"].ToString();
        if (!string.IsNullOrEmpty(queryId) && int.TryParse(queryId, out var parsedId))
            return parsedId;
        return null;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        try
        {
            var patientId = await ResolvePatientIdAsync();
            if (patientId == null)
            {
                ErrorMessage = "Patient profile not found. Please ensure your account is linked to a patient record. Contact reception if you need assistance.";
                IsLoading = false;
                return Page();
            }
            CurrentPatientId = patientId.Value;

            // Load dashboard data
            Dashboard = await _dashboardService.GetPatientDashboardAsync(CurrentPatientId);

            if (Dashboard != null)
            {
                UpcomingAppointments = Dashboard.UpcomingAppointments ?? new();
                RecentPrescriptions = Dashboard.RecentPrescriptions ?? new();
                PendingLabTests = Dashboard.RecentLabTests ?? new();
                OutstandingInvoices = Dashboard.OutstandingInvoices ?? new();
            }

            IsLoading = false;
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading dashboard for patient {PatientId}", CurrentPatientId);
            ErrorMessage = "An error occurred while loading your dashboard. Please try again later.";
            IsLoading = false;
            return Page();
        }
    }

    /// <summary>
    /// Handler for viewing appointment history
    /// </summary>
    public async Task<IActionResult> OnGetAppointmentHistoryAsync(int pageNumber = 1)
    {
        var patientId = await ResolvePatientIdAsync();
        if (patientId == null) return new JsonResult(new { error = "Unauthorized" }) { StatusCode = 401 };
        CurrentPatientId = patientId.Value;
        try
        {
            var (items, totalCount) = await _dashboardService.GetAppointmentHistoryAsync(
                CurrentPatientId,
                pageNumber,
                pageSize: 10);

            return new JsonResult(new { items, totalCount, pageNumber });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading appointment history");
            return new JsonResult(new { error = "Failed to load appointment history" }) 
            { 
                StatusCode = StatusCodes.Status500InternalServerError 
            };
        }
    }

    /// <summary>
    /// Handler for viewing prescription history
    /// </summary>
    public async Task<IActionResult> OnGetPrescriptionHistoryAsync(int pageNumber = 1)
    {
        var patientId = await ResolvePatientIdAsync();
        if (patientId == null) return new JsonResult(new { error = "Unauthorized" }) { StatusCode = 401 };
        CurrentPatientId = patientId.Value;
        try
        {
            var (items, totalCount) = await _dashboardService.GetPrescriptionHistoryAsync(
                CurrentPatientId,
                pageNumber,
                pageSize: 10);

            return new JsonResult(new { items, totalCount, pageNumber });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading prescription history");
            return new JsonResult(new { error = "Failed to load prescription history" }) 
            { 
                StatusCode = StatusCodes.Status500InternalServerError 
            };
        }
    }

    /// <summary>
    /// Handler for viewing lab test history
    /// </summary>
    public async Task<IActionResult> OnGetLabTestHistoryAsync(int pageNumber = 1)
    {
        var patientId = await ResolvePatientIdAsync();
        if (patientId == null) return new JsonResult(new { error = "Unauthorized" }) { StatusCode = 401 };
        CurrentPatientId = patientId.Value;
        try
        {
            var (items, totalCount) = await _dashboardService.GetLabTestHistoryAsync(
                CurrentPatientId,
                pageNumber,
                pageSize: 10);

            return new JsonResult(new { items, totalCount, pageNumber });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading lab test history");
            return new JsonResult(new { error = "Failed to load lab test history" }) 
            { 
                StatusCode = StatusCodes.Status500InternalServerError 
            };
        }
    }

    /// <summary>
    /// Handler for viewing outstanding invoices
    /// </summary>
    public async Task<IActionResult> OnGetOutstandingInvoicesAsync(int pageNumber = 1)
    {
        var patientId = await ResolvePatientIdAsync();
        if (patientId == null) return new JsonResult(new { error = "Unauthorized" }) { StatusCode = 401 };
        CurrentPatientId = patientId.Value;
        try
        {
            var (items, totalCount) = await _dashboardService.GetOutstandingInvoicesAsync(
                CurrentPatientId,
                pageNumber,
                pageSize: 10);

            return new JsonResult(new { items, totalCount, pageNumber });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading outstanding invoices");
            return new JsonResult(new { error = "Failed to load outstanding invoices" }) 
            { 
                StatusCode = StatusCodes.Status500InternalServerError 
            };
        }
    }
}
