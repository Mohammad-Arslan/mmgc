using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MMGC.Data;
using MMGC.Models;
using MMGC.Services;

namespace MMGC.Controllers;

[Authorize]
public class PatientsController : Controller
{
    private readonly IPatientService _patientService;
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public PatientsController(
        IPatientService patientService,
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _patientService = patientService;
        _context = context;
        _userManager = userManager;
    }

    // Helper to get current nurse
    private async Task<Nurse?> GetCurrentNurseAsync()
    {
        if (!User.IsInRole("Nurse")) return null;
        
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return null;

        var nurse = await _context.Nurses
            .FirstOrDefaultAsync(n => n.UserId == user.Id);
        
        if (nurse == null && !string.IsNullOrEmpty(user.Email))
        {
            var allNurses = await _context.Nurses.ToListAsync();
            nurse = allNurses.FirstOrDefault(n => n.Email?.ToLower() == user.Email.ToLower());
        }

        return nurse;
    }

    // GET: Patients
    public async Task<IActionResult> Index()
    {
        // If user is a nurse, show only patients from assigned appointments/procedures
        if (User.IsInRole("Nurse"))
        {
            var currentNurse = await GetCurrentNurseAsync();
            if (currentNurse == null)
            {
                ViewBag.ErrorMessage = "Nurse profile not found. Please contact administrator to link your user account to a nurse profile.";
                return View(new List<Patient>());
            }

            // Get patient IDs from appointments and procedures assigned to this nurse
            var appointmentPatientIds = await _context.Appointments
                .Where(a => a.NurseId == currentNurse.Id && a.PatientId.HasValue)
                .Select(a => a.PatientId!.Value)
                .Distinct()
                .ToListAsync();

            var procedurePatientIds = await _context.Procedures
                .Where(p => p.NurseId == currentNurse.Id && p.PatientId.HasValue)
                .Select(p => p.PatientId!.Value)
                .Distinct()
                .ToListAsync();

            var allPatientIds = appointmentPatientIds.Union(procedurePatientIds).Distinct().ToList();

            var patients = await _context.Patients
                .Where(p => allPatientIds.Contains(p.Id))
                .OrderBy(p => p.LastName)
                .ThenBy(p => p.FirstName)
                .ToListAsync();

            return View(patients);
        }

        // For Admin and other roles, show all patients
        var allPatients = await _patientService.GetAllPatientsAsync();
        return View(allPatients);
    }

    // GET: Patients/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var patient = await _patientService.GetPatientWithHistoryAsync(id.Value);
        if (patient == null)
        {
            return NotFound();
        }

        return View(patient);
    }

    // GET: Patients/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: Patients/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("FirstName,LastName,ContactNumber,AlternateContact,Email,DateOfBirth,Gender,Address,City,State,PostalCode,MedicalHistory,Allergies")] Patient patient)
    {
        // Remove MRNumber from ModelState as it's auto-generated
        ModelState.Remove("MRNumber");
        
        if (ModelState.IsValid)
        {
            try
            {
                await _patientService.CreatePatientAsync(patient);
                TempData["SuccessMessage"] = "Patient registered successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error creating patient: {ex.Message}");
            }
        }
        
        // Log validation errors for debugging
        foreach (var modelState in ModelState.Values)
        {
            foreach (var error in modelState.Errors)
            {
                System.Diagnostics.Debug.WriteLine($"Validation Error: {error.ErrorMessage}");
            }
        }
        
        return View(patient);
    }

    // GET: Patients/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var patient = await _patientService.GetPatientByIdAsync(id.Value);
        if (patient == null)
        {
            return NotFound();
        }
        return View(patient);
    }

    // POST: Patients/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,MRNumber,FirstName,LastName,ContactNumber,AlternateContact,Email,DateOfBirth,Gender,Address,City,State,PostalCode,MedicalHistory,Allergies")] Patient patient)
    {
        if (id != patient.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                await _patientService.UpdatePatientAsync(patient);
                TempData["SuccessMessage"] = "Patient updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                if (!await PatientExists(patient.Id))
                {
                    return NotFound();
                }
                ModelState.AddModelError("", $"Error updating patient: {ex.Message}");
            }
        }
        return View(patient);
    }

    // GET: Patients/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var patient = await _patientService.GetPatientByIdAsync(id.Value);
        if (patient == null)
        {
            return NotFound();
        }

        return View(patient);
    }

    // POST: Patients/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        try
        {
            await _patientService.DeletePatientAsync(id);
            TempData["SuccessMessage"] = "Patient deleted successfully!";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Error deleting patient: {ex.Message}";
        }
        return RedirectToAction(nameof(Index));
    }

    private async Task<bool> PatientExists(int id)
    {
        var patient = await _patientService.GetPatientByIdAsync(id);
        return patient != null;
    }
}
