using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
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
    private readonly ILogger<RequestModel> _logger;

    [BindProperty]
    public string? ProcedureType { get; set; }

    [BindProperty]
    public string? ReasonForProcedure { get; set; }

    [BindProperty]
    public DateTime? RequestedDate { get; set; }

    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }
    public int? CreatedRequestId { get; set; }
    public List<string> AvailableProcedures { get; set; } = new()
    {
        "Appendectomy",
        "Cholecystectomy",
        "Hernia Repair",
        "Knee Replacement",
        "Hip Replacement",
        "Cataract Surgery",
        "Prostate Surgery",
        "Cesarean Section",
        "Hysterectomy",
        "Colonoscopy",
        "Endoscopy",
        "Laparoscopy"
    };

    public RequestModel(
        IProcedureWorkflowService workflowService,
        INotificationService notificationService,
        ILogger<RequestModel> logger)
    {
        _workflowService = workflowService;
        _notificationService = notificationService;
        _logger = logger;
    }

    public void OnGet()
    {
        RequestedDate = DateTime.Now.AddDays(7);
    }

    public async Task<IActionResult> OnPostAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(ProcedureType))
            {
                ErrorMessage = "Please select a procedure type.";
                return Page();
            }

            if (string.IsNullOrWhiteSpace(ReasonForProcedure))
            {
                ErrorMessage = "Please provide a reason for the procedure request.";
                return Page();
            }

            if (ReasonForProcedure.Length < 10)
            {
                ErrorMessage = "Please provide a detailed reason (at least 10 characters).";
                return Page();
            }

            // TODO: Get patient ID from current user
            int patientId = 1; // Placeholder

            // Create procedure request
            var request = await _workflowService.CreateProcedureRequestAsync(
                patientId,
                ProcedureType,
                ReasonForProcedure,
                RequestedDate);

            CreatedRequestId = request.Id;
            SuccessMessage = $"Procedure request submitted successfully! Request ID: {request.Id}. Your doctor will review it shortly.";

            // Clear form
            ProcedureType = null;
            ReasonForProcedure = null;
            RequestedDate = null;

            _logger.LogInformation("Procedure request created: ID={RequestId}, Type={Type}, Patient={PatientId}",
                request.Id, ProcedureType, patientId);

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating procedure request");
            ErrorMessage = "An error occurred while submitting your request. Please try again.";
            return Page();
        }
    }
}
