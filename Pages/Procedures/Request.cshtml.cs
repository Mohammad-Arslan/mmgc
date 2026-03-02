using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MMGC.Data;
using MMGC.Shared.Exceptions;
using MMGC.Shared.Interfaces;

namespace MMGC.Pages.Procedures;

/// <summary>
/// PageModel for requesting a medical procedure.
/// Allows patients to request procedures which are then reviewed by doctors.
/// </summary>
[Authorize(Roles = "Patient")]
public class RequestModel : PageModel
{
    private readonly IProcedureWorkflowService _workflowService;
    private readonly INotificationService _notificationService;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<RequestModel> _logger;

    [BindProperty]
    public int? DoctorId { get; set; }

    [BindProperty]
    public string? ProcedureType { get; set; }

    [BindProperty]
    public string? ReasonForProcedure { get; set; }

    [BindProperty]
    public DateTime? RequestedDate { get; set; }

    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }
    public int? CreatedRequestId { get; set; }
    public List<DoctorOption> AvailableDoctors { get; set; } = new();
    public List<string> AvailableProcedures { get; set; } = new();

    public record DoctorOption(int Id, string FullName);

    public RequestModel(
        IProcedureWorkflowService workflowService,
        INotificationService notificationService,
        ApplicationDbContext context,
        ILogger<RequestModel> logger)
    {
        _workflowService = workflowService;
        _notificationService = notificationService;
        _context = context;
        _logger = logger;
    }

    private async Task<int?> GetPatientIdAsync()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return null;
        var patient = await _context.Patients.AsNoTracking().FirstOrDefaultAsync(p => p.UserId == userId);
        return patient?.Id;
    }

    public async Task OnGetAsync()
    {
        RequestedDate = DateTime.Now.AddDays(7);
        await LoadDropdownDataAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        try
        {
            if (!DoctorId.HasValue || DoctorId <= 0)
            {
                ErrorMessage = "Please select a doctor.";
                await LoadDropdownDataAsync();
                return Page();
            }

            if (string.IsNullOrWhiteSpace(ProcedureType))
            {
                ErrorMessage = "Please select a procedure type.";
                await LoadDropdownDataAsync();
                return Page();
            }

            if (string.IsNullOrWhiteSpace(ReasonForProcedure))
            {
                ErrorMessage = "Please provide a reason for the procedure request.";
                await LoadDropdownDataAsync();
                return Page();
            }

            if (ReasonForProcedure.Length < 10)
            {
                ErrorMessage = "Please provide a detailed reason (at least 10 characters).";
                await LoadDropdownDataAsync();
                return Page();
            }

            var patientId = await GetPatientIdAsync();
            if (!patientId.HasValue)
            {
                ErrorMessage = "Patient profile not found. Please ensure your account is linked to a patient record.";
                return Page();
            }

            await LoadDropdownDataAsync();

            var request = await _workflowService.CreateProcedureRequestAsync(
                patientId.Value,
                DoctorId,
                ProcedureType,
                ReasonForProcedure,
                RequestedDate);

            CreatedRequestId = request.Id;
            SuccessMessage = $"Procedure request submitted successfully! Request ID: {request.Id}. Your doctor will review it shortly.";

            // Clear form
            DoctorId = null;
            ProcedureType = null;
            ReasonForProcedure = null;
            RequestedDate = null;

            _logger.LogInformation("Procedure request created: ID={RequestId}, Type={Type}, Patient={PatientId}",
                request.Id, ProcedureType, patientId);

            return Page();
        }
        catch (EntityNotFoundException ex)
        {
            _logger.LogWarning(ex, "Entity not found when creating procedure request");
            ErrorMessage = ex.Message;
            await LoadDropdownDataAsync();
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating procedure request");
            // Surface the actual error so user/admin can see what went wrong
            var detail = ex.InnerException?.Message ?? ex.Message;
            ErrorMessage = $"An error occurred: {detail}";
            await LoadDropdownDataAsync();
            return Page();
        }
    }

    private async Task LoadDropdownDataAsync()
    {
        AvailableDoctors = await _context.Doctors
            .AsNoTracking()
            .Where(d => d.IsActive)
            .OrderBy(d => d.LastName)
            .Select(d => new DoctorOption(d.Id, d.FullName))
            .ToListAsync();

        AvailableProcedures = await _context.Procedures
            .AsNoTracking()
            .Select(p => p.ProcedureName)
            .Distinct()
            .OrderBy(n => n)
            .ToListAsync();
    }
}
