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
public class ProfileModel : PageModel
{
    private readonly IPatientDashboardService _dashboardService;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ProfileModel> _logger;

    public PatientDashboardDto? Profile { get; set; }
    public string? ErrorMessage { get; set; }

    public ProfileModel(
        IPatientDashboardService dashboardService,
        ApplicationDbContext context,
        ILogger<ProfileModel> logger)
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
        return patient?.Id;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var patientId = await ResolvePatientIdAsync();
        if (patientId == null)
        {
            ErrorMessage = "Patient profile not found. Please ensure your account is linked to a patient record.";
            return Page();
        }

        try
        {
            Profile = await _dashboardService.GetPatientDashboardAsync(patientId.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading profile for patient {PatientId}", patientId);
            ErrorMessage = "An error occurred while loading your profile.";
        }

        return Page();
    }
}
