using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MMGC.Data;
using MMGC.Models;
using MMGC.Services;
using MMGC.Shared.Interfaces;

namespace MMGC.Controllers;

[Authorize(Roles = "Doctor")]
public class DoctorDashboardController : Controller
{
    private readonly IDoctorDashboardService _dashboardService;
    private readonly IDoctorService _doctorService;
    private readonly IProcedureWorkflowService _procedureWorkflowService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<DoctorDashboardController> _logger;

    public DoctorDashboardController(
        IDoctorDashboardService dashboardService,
        IDoctorService doctorService,
        IProcedureWorkflowService procedureWorkflowService,
        UserManager<ApplicationUser> userManager,
        ILogger<DoctorDashboardController> logger)
    {
        _dashboardService = dashboardService;
        _doctorService = doctorService;
        _procedureWorkflowService = procedureWorkflowService;
        _userManager = userManager;
        _logger = logger;
    }

    private async Task<Doctor?> GetCurrentDoctorAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return null;

        // Try to find doctor by UserId
        var doctor = await _doctorService.GetDoctorByUserIdAsync(user.Id);
        
        // If not found by UserId, try to find by email
        if (doctor == null && !string.IsNullOrEmpty(user.Email))
        {
            var allDoctors = await _doctorService.GetAllDoctorsAsync();
            doctor = allDoctors.FirstOrDefault(d => d.Email?.ToLower() == user.Email.ToLower());
        }

        return doctor;
    }

    // GET: DoctorDashboard
    public async Task<IActionResult> Index()
    {
        var doctor = await GetCurrentDoctorAsync();
        if (doctor == null)
        {
            // Show error message on the dashboard instead of redirecting to avoid loop
            ViewBag.ErrorMessage = "Doctor profile not found. Please contact administrator to link your user account to a doctor profile.";
            ViewBag.Statistics = new Dictionary<string, object>
            {
                { "TotalAppointments", 0 },
                { "TodayAppointments", 0 },
                { "ThisMonthAppointments", 0 },
                { "TotalProcedures", 0 },
                { "TotalPatients", 0 },
                { "MonthlyRevenue", 0 },
                { "PendingProcedureRequests", 0 }
            };
            ViewBag.RecentAppointments = new List<Appointment>();
            return View(new Doctor { FirstName = "Doctor", LastName = "Profile Not Found" });
        }

        var stats = await _dashboardService.GetDashboardStatisticsAsync(doctor.Id);
        var recentAppointments = await _dashboardService.GetAppointmentsHistoryAsync(doctor.Id);
        
        ViewBag.Statistics = stats;
        ViewBag.Doctor = doctor;
        ViewBag.RecentAppointments = recentAppointments.Take(10);

        return View(doctor);
    }

    // GET: DoctorDashboard/Profile
    public async Task<IActionResult> Profile()
    {
        var doctor = await GetCurrentDoctorAsync();
        if (doctor == null)
        {
            return NotFound();
        }

        var profile = await _dashboardService.GetDoctorProfileAsync(doctor.Id);
        return View(profile);
    }

    // POST: DoctorDashboard/Profile
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(int id, [Bind("Id,FirstName,LastName,Specialization,ContactNumber,Email,LicenseNumber,Address,ConsultationFee")] Doctor doctor)
    {
        if (id != doctor.Id)
        {
            return NotFound();
        }

        var currentDoctor = await GetCurrentDoctorAsync();
        if (currentDoctor == null || currentDoctor.Id != id)
        {
            return Forbid();
        }

        if (ModelState.IsValid)
        {
            try
            {
                await _doctorService.UpdateDoctorAsync(doctor);
                TempData["SuccessMessage"] = "Profile updated successfully!";
                return RedirectToAction(nameof(Profile));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating doctor profile");
                ModelState.AddModelError("", $"Error updating profile: {ex.Message}");
            }
        }

        var profile = await _dashboardService.GetDoctorProfileAsync(doctor.Id);
        return View(profile);
    }

    // GET: DoctorDashboard/AppointmentsHistory
    public async Task<IActionResult> AppointmentsHistory(DateTime? fromDate, DateTime? toDate)
    {
        var doctor = await GetCurrentDoctorAsync();
        if (doctor == null)
        {
            return NotFound();
        }

        var appointments = await _dashboardService.GetAppointmentsHistoryAsync(doctor.Id, fromDate, toDate);
        ViewBag.Doctor = doctor;
        ViewBag.FromDate = fromDate;
        ViewBag.ToDate = toDate;

        return View(appointments);
    }

    // GET: DoctorDashboard/Patients
    public async Task<IActionResult> Patients()
    {
        var doctor = await GetCurrentDoctorAsync();
        if (doctor == null)
        {
            return NotFound();
        }

        var patients = await _dashboardService.GetPatientsListAsync(doctor.Id);
        ViewBag.Doctor = doctor;

        return View(patients);
    }

    // GET: DoctorDashboard/PatientHistory/5
    public async Task<IActionResult> PatientHistory(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var doctor = await GetCurrentDoctorAsync();
        if (doctor == null)
        {
            return NotFound();
        }

        var patient = await _dashboardService.GetPatientWithCompleteHistoryAsync(id.Value, doctor.Id);
        if (patient == null)
        {
            return NotFound();
        }

        ViewBag.Doctor = doctor;
        return View(patient);
    }

    // GET: DoctorDashboard/ProcedureRequests
    public async Task<IActionResult> ProcedureRequests(int page = 1, int pageSize = 10)
    {
        var doctor = await GetCurrentDoctorAsync();
        if (doctor == null) return NotFound();

        var (items, totalCount) = await _procedureWorkflowService.GetPendingRequestsForDoctorAsync(doctor.Id, page, pageSize);
        ViewBag.Doctor = doctor;
        ViewBag.TotalCount = totalCount;
        ViewBag.Page = page;
        ViewBag.PageSize = pageSize;
        ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        return View(items);
    }

    // POST: DoctorDashboard/ApproveProcedureRequest/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveProcedureRequest(int id, string? approvalComments, DateTime? scheduledDate)
    {
        var doctor = await GetCurrentDoctorAsync();
        if (doctor == null) return NotFound();

        try
        {
            await _procedureWorkflowService.ApproveProcedureRequestAsync(id, doctor.Id, approvalComments, scheduledDate);
            TempData["SuccessMessage"] = "Procedure request approved successfully.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving procedure request {Id}", id);
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(ProcedureRequests));
    }

    // POST: DoctorDashboard/RejectProcedureRequest/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectProcedureRequest(int id, string rejectionReason)
    {
        var doctor = await GetCurrentDoctorAsync();
        if (doctor == null) return NotFound();

        try
        {
            await _procedureWorkflowService.RejectProcedureRequestAsync(id, doctor.Id, rejectionReason ?? "No reason provided.");
            TempData["SuccessMessage"] = "Procedure request rejected.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting procedure request {Id}", id);
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(ProcedureRequests));
    }
}

