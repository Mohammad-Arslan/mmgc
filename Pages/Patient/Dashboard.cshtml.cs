using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
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
    private readonly ILogger<DashboardModel> _logger;

    public PatientDashboardDto? Dashboard { get; set; }
    public List<DashboardItemDto> UpcomingAppointments { get; set; } = new();
    public List<DashboardItemDto> RecentPrescriptions { get; set; } = new();
    public List<DashboardItemDto> PendingLabTests { get; set; } = new();
    public List<DashboardItemDto> OutstandingInvoices { get; set; } = new();

    public int CurrentPatientId { get; private set; }
    public string? ErrorMessage { get; set; }
    public bool IsLoading { get; set; } = true;

    public DashboardModel(IPatientDashboardService dashboardService, ILogger<DashboardModel> logger)
    {
        _dashboardService = dashboardService;
        _logger = logger;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        try
        {
            // Get current user's patient ID
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
            {
                ErrorMessage = "Unable to identify current user.";
                return Unauthorized();
            }

            // In a real system, you'd map the UserId to PatientId
            // For now, we'll use the first parameter or query string
            CurrentPatientId = HttpContext.Request.Query["patientId"].ToString() switch
            {
                "" or null => 1, // Default for demo
                string id when int.TryParse(id, out var parsedId) => parsedId,
                _ => 1
            };

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
