using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MMGC.Data;
using MMGC.Shared.DTOs;
using MMGC.Shared.Interfaces;
using System.Security.Claims;

namespace MMGC.Pages.Patient;

[Authorize(Roles = "Patient")]
public class AppointmentsModel : PageModel
{
    private readonly IPatientDashboardService _dashboardService;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AppointmentsModel> _logger;

    public AppointmentsModel(
        IPatientDashboardService dashboardService,
        ApplicationDbContext context,
        ILogger<AppointmentsModel> logger)
    {
        _dashboardService = dashboardService;
        _context = context;
        _logger = logger;
    }

    public List<DashboardItemDto> Appointments { get; set; } = new();
    public int TotalCount { get; set; }
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public string? ErrorMessage { get; set; }

    private async Task<int?> ResolvePatientIdAsync()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim)) return null;

        var patient = await _context.Patients
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userIdClaim);
        return patient?.Id;
    }

    public async Task<IActionResult> OnGetAsync(int pageNumber = 1)
    {
        var patientId = await ResolvePatientIdAsync();
        if (patientId == null)
        {
            ErrorMessage = "Patient profile not found.";
            return Page();
        }

        CurrentPage = Math.Max(1, pageNumber);

        try
        {
            var (items, totalCount) = await _dashboardService.GetAppointmentHistoryAsync(
                patientId.Value,
                CurrentPage,
                PageSize);
            Appointments = items;
            TotalCount = totalCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading appointments for patient {PatientId}", patientId);
            ErrorMessage = "An error occurred while loading your appointments.";
        }

        return Page();
    }
}
