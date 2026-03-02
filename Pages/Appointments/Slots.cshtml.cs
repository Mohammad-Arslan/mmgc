using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MMGC.Data;
using MMGC.Shared.DTOs;
using MMGC.Shared.Interfaces;

namespace MMGC.Pages.Appointments;

/// <summary>
/// PageModel for selecting and booking appointment slots.
/// Displays available slots for a selected doctor and date.
/// </summary>
[Authorize(Roles = "Patient")]
public class SlotsModel : PageModel
{
    private readonly IAvailabilityService _availabilityService;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SlotsModel> _logger;

    [BindProperty(SupportsGet = true)]
    public int DoctorId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SelectedDate { get; set; }

    [BindProperty]
    public string? SelectedSlotId { get; set; }

    [BindProperty]
    public string? Reason { get; set; }

    public List<AppointmentSlotDto> AvailableSlots { get; set; } = new();
    public string? DoctorName { get; set; }
    public string? Specialization { get; set; }
    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }
    public DateTime MinDate { get; set; }
    public DateTime MaxDate { get; set; }

    public SlotsModel(
        IAvailabilityService availabilityService,
        ApplicationDbContext context,
        ILogger<SlotsModel> logger)
    {
        _availabilityService = availabilityService;
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

    public async Task<IActionResult> OnGetAsync()
    {
        try
        {
            MinDate = DateTime.Now.Date;
            MaxDate = DateTime.Now.Date.AddDays(30);

            if (DoctorId <= 0)
            {
                ErrorMessage = "Please select a doctor first.";
                return Page();
            }

            if (string.IsNullOrEmpty(SelectedDate))
            {
                SelectedDate = MinDate.ToString("yyyy-MM-dd");
            }

            // Parse selected date
            if (!DateTime.TryParse(SelectedDate, out var selectedDateTime))
            {
                ErrorMessage = "Invalid date selected.";
                return Page();
            }

            // Get available slots (includes doctor name and specialization)
            AvailableSlots = await _availabilityService.GetAvailableSlotsAsync(DoctorId, selectedDateTime);
            if (AvailableSlots.Count > 0)
            {
                DoctorName = AvailableSlots[0].DoctorName;
                Specialization = AvailableSlots[0].Specialization;
            }

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading available slots for doctor {DoctorId}", DoctorId);
            ErrorMessage = "An error occurred while loading available slots.";
            return Page();
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        try
        {
            if (DoctorId <= 0 || string.IsNullOrWhiteSpace(SelectedSlotId))
            {
                ErrorMessage = "Please select a valid slot.";
                return Page();
            }

            if (string.IsNullOrWhiteSpace(Reason))
            {
                ErrorMessage = "Please provide a reason for the appointment.";
                return Page();
            }

            // Get patient ID from current user
            var patientId = await GetPatientIdAsync();
            if (!patientId.HasValue)
            {
                ErrorMessage = "Patient profile not found. Please ensure your account is linked to a patient record.";
                return Page();
            }

            // Get slot details to extract start and end time
            if (!DateTime.TryParse(SelectedDate, out var selectedDateTime))
            {
                ErrorMessage = "Invalid date selected.";
                return Page();
            }

            var slots = await _availabilityService.GetAvailableSlotsAsync(DoctorId, selectedDateTime);
            var selectedSlot = slots.FirstOrDefault(s => s.SlotId == SelectedSlotId);

            if (selectedSlot == null)
            {
                ErrorMessage = "Selected slot is no longer available.";
                return Page();
            }

            // Convert TimeSpan to DateTime by combining with the selected date
            var startDateTime = selectedSlot.ScheduleDate.Date + selectedSlot.StartTime;
            var endDateTime = selectedSlot.ScheduleDate.Date + selectedSlot.EndTime;

            // Book the appointment
            var appointmentId = await _availabilityService.ReserveSlotAsync(
                DoctorId,
                patientId.Value,
                startDateTime,
                endDateTime);

            SuccessMessage = $"Appointment booked successfully! Your appointment ID is {appointmentId}.";
            
            // Send confirmation notification
            // await _notificationService.SendAppointmentConfirmationAsync(appointmentId);

            return RedirectToPage("/Patient/Dashboard");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error booking appointment");
            ErrorMessage = "An error occurred while booking your appointment. Please try again.";
            return Page();
        }
    }
}
