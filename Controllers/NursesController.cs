using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MMGC.Data;
using MMGC.Models;
using MMGC.Repositories;
using MMGC.Services;

namespace MMGC.Controllers;

[Authorize(Roles = "Nurse,Admin")]
public class NursesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IRepository<Nurse> _nurseRepository;
    private readonly IPatientService _patientService;
    private readonly IProcedureService _procedureService;
    private readonly IAppointmentService _appointmentService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<NursesController> _logger;

    public NursesController(
        ApplicationDbContext context,
        IRepository<Nurse> nurseRepository,
        IPatientService patientService,
        IProcedureService procedureService,
        IAppointmentService appointmentService,
        UserManager<ApplicationUser> userManager,
        ILogger<NursesController> logger)
    {
        _context = context;
        _nurseRepository = nurseRepository;
        _patientService = patientService;
        _procedureService = procedureService;
        _appointmentService = appointmentService;
        _userManager = userManager;
        _logger = logger;
    }

    private async Task<Nurse?> GetCurrentNurseAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            _logger.LogWarning("GetCurrentNurseAsync: User is null");
            return null;
        }

        // Try to find nurse by UserId first
        var nurse = await _context.Nurses
            .FirstOrDefaultAsync(n => n.UserId == user.Id);
        
        if (nurse != null)
        {
            _logger.LogDebug("GetCurrentNurseAsync: Found nurse {NurseId} by UserId {UserId}", nurse.Id, user.Id);
            return nurse;
        }
        
        // If not found by UserId, try to find by email and link it
        if (!string.IsNullOrEmpty(user.Email))
        {
            nurse = await _context.Nurses
                .FirstOrDefaultAsync(n => n.Email != null && n.Email.ToLower() == user.Email.ToLower());
            
            if (nurse != null)
            {
                // Link the nurse profile to the user by setting UserId
                if (string.IsNullOrEmpty(nurse.UserId))
                {
                    _logger.LogInformation("Linking nurse profile {NurseId} to user {UserId} by email {Email}", 
                        nurse.Id, user.Id, user.Email);
                    nurse.UserId = user.Id;
                    nurse.UpdatedDate = DateTime.Now;
                    await _context.SaveChangesAsync();
                }
                else if (nurse.UserId != user.Id)
                {
                    _logger.LogWarning("Nurse profile {NurseId} has different UserId {NurseUserId} than current user {UserId}", 
                        nurse.Id, nurse.UserId, user.Id);
                }
                
                return nurse;
            }
        }

        _logger.LogWarning("GetCurrentNurseAsync: No nurse profile found for user {UserId} with email {Email}", 
            user.Id, user.Email);
        return null;
    }

    // GET: Nurses/Dashboard
    public async Task<IActionResult> Dashboard()
    {
        var currentNurse = await GetCurrentNurseAsync();
        
        if (currentNurse == null && !User.IsInRole("Admin"))
        {
            ViewBag.ErrorMessage = "Nurse profile not found. Please contact administrator to link your user account to a nurse profile.";
            ViewBag.ActiveNurses = 0;
            ViewBag.TotalProcedures = 0;
            ViewBag.TodayNotes = 0;
            ViewBag.MyProcedures = 0;
            ViewBag.MyNotes = 0;
            ViewBag.MyVitals = 0;
            return View();
        }

        var nurses = await _nurseRepository.GetAllAsync();
        var activeNurses = nurses.Where(n => n.IsActive).Count();
        var totalProcedures = await _context.Procedures.CountAsync();
        
        // Get nurse-specific statistics if nurse is found
        int todayNotes = 0;
        int myProcedures = 0;
        int myNotes = 0;
        int myVitals = 0;
        
        if (currentNurse != null)
        {
            todayNotes = await _context.NursingNotes
                .Where(n => n.NurseId == currentNurse.Id && n.NoteDate.Date == DateTime.Today)
                .CountAsync();
            
            myProcedures = await _context.Procedures
                .Where(p => p.NurseId == currentNurse.Id)
                .CountAsync();
            
            myNotes = await _context.NursingNotes
                .Where(n => n.NurseId == currentNurse.Id)
                .CountAsync();
            
            myVitals = await _context.PatientVitals
                .Where(v => v.NurseId == currentNurse.Id)
                .CountAsync();
        }
        else if (User.IsInRole("Admin"))
        {
            // Admin sees all statistics
            todayNotes = await _context.NursingNotes
                .Where(n => n.NoteDate.Date == DateTime.Today)
                .CountAsync();
        }

        ViewBag.Nurse = currentNurse;
        ViewBag.ActiveNurses = activeNurses;
        ViewBag.TotalProcedures = totalProcedures;
        ViewBag.TodayNotes = todayNotes;
        ViewBag.MyProcedures = myProcedures;
        ViewBag.MyNotes = myNotes;
        ViewBag.MyVitals = myVitals;

        return View();
    }

    // GET: Nurses/NursingNotes
    public async Task<IActionResult> NursingNotes(int? patientId, int? procedureId)
    {
        var currentNurse = await GetCurrentNurseAsync();
        
        var query = _context.NursingNotes
            .Include(n => n.Patient)
            .Include(n => n.Procedure)
            .Include(n => n.Appointment)
            .Include(n => n.Nurse)
            .AsQueryable();

        // For nurses, only show their own notes
        if (currentNurse != null && !User.IsInRole("Admin"))
        {
            query = query.Where(n => n.NurseId == currentNurse.Id);
        }

        if (patientId.HasValue)
        {
            query = query.Where(n => n.PatientId == patientId.Value);
        }

        if (procedureId.HasValue)
        {
            query = query.Where(n => n.ProcedureId == procedureId.Value);
        }

        var notes = await query.OrderByDescending(n => n.NoteDate).ToListAsync();
        return View(notes);
    }

    // GET: Nurses/CreateNursingNote
    public async Task<IActionResult> CreateNursingNote(int? patientId, int? procedureId, int? appointmentId)
    {
        await PopulateDropDownsAsync(patientId, procedureId, appointmentId);
        
        var currentNurse = await GetCurrentNurseAsync();
        
        // Check if there are any patients available
        var patientsList = ViewBag.Patients as SelectList;
        if (patientsList == null || patientsList.Count() == 0)
        {
            if (currentNurse != null && !User.IsInRole("Admin"))
            {
                TempData["ErrorMessage"] = "No patients are assigned to you. Please contact administrator to assign patients or appointments.";
            }
            else
            {
                TempData["ErrorMessage"] = "No patients available. Please register patients first.";
            }
            return RedirectToAction(nameof(NursingNotes));
        }
        
        var note = new NursingNote
        {
            PatientId = patientId,
            ProcedureId = procedureId,
            AppointmentId = appointmentId,
            NoteDate = DateTime.Now,
            NurseId = null  // Initialize to null, will be set below
        };

        // Auto-assign current nurse
        if (currentNurse != null)
        {
            note.NurseId = currentNurse.Id;
            _logger.LogInformation("Pre-selected nurse {NurseId} for new note", currentNurse.Id);
        }
        else if (!User.IsInRole("Admin"))
        {
            TempData["ErrorMessage"] = "Nurse profile not found. Please contact administrator to link your user account to a nurse profile.";
            return RedirectToAction(nameof(Dashboard));
        }

        return View(note);
    }

    // POST: Nurses/CreateNursingNote
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateNursingNote(NursingNote note)
    {
        // Log all form values for debugging
        _logger.LogInformation("CreateNursingNote POST called. PatientId: {PatientId}, NurseId: {NurseId}, ProcedureId: {ProcedureId}, AppointmentId: {AppointmentId}", 
            note.PatientId, note.NurseId, note.ProcedureId, note.AppointmentId);
        
        // Try to manually read form values if model binding failed (fallback)
        if ((!note.PatientId.HasValue || note.PatientId <= 0) && Request.Form.ContainsKey("PatientId"))
        {
            var patientIdStr = Request.Form["PatientId"].ToString();
            if (int.TryParse(patientIdStr, out int parsedPatientId) && parsedPatientId > 0)
            {
                note.PatientId = parsedPatientId;
                _logger.LogInformation("Manually parsed PatientId from form: {PatientId}", parsedPatientId);
                // Clear any validation errors for PatientId since we've set it
                ModelState.Remove("PatientId");
            }
        }
        
        if ((!note.NurseId.HasValue || note.NurseId <= 0) && Request.Form.ContainsKey("NurseId"))
        {
            var nurseIdStr = Request.Form["NurseId"].ToString();
            if (int.TryParse(nurseIdStr, out int parsedNurseId) && parsedNurseId > 0)
            {
                note.NurseId = parsedNurseId;
                _logger.LogInformation("Manually parsed NurseId from form: {NurseId}", parsedNurseId);
                // Clear any validation errors for NurseId since we've set it
                ModelState.Remove("NurseId");
            }
        }
        
        // Log raw form data
        _logger.LogInformation("Form data - PatientId from Request: {PatientId}, NurseId from Request: {NurseId}",
            Request.Form["PatientId"].ToString(), Request.Form["NurseId"].ToString());

        // Auto-assign current nurse if not set
        var currentNurse = await GetCurrentNurseAsync();
        if (currentNurse != null)
        {
            // If NurseId is null or 0 or not set, auto-assign
            if (!note.NurseId.HasValue || note.NurseId <= 0)
            {
                note.NurseId = currentNurse.Id;
                _logger.LogInformation("Auto-assigned nurse {NurseId} to note", currentNurse.Id);
                // Remove any existing NurseId validation error since we're auto-assigning
                ModelState.Remove("NurseId");
            }
        }
        else if (!User.IsInRole("Admin"))
        {
            // Only add error if nurse is still not set after auto-assignment attempt
            if (!note.NurseId.HasValue || note.NurseId <= 0)
            {
                ModelState.AddModelError("", "Nurse profile not found. Please contact administrator to link your user account to a nurse profile.");
                _logger.LogWarning("No nurse profile found for current user");
            }
        }
        
        // Clear ModelState errors for PatientId and NurseId FIRST if they have valid values
        // This prevents [Required] and [Range] attribute validation errors from blocking submission
        if (note.PatientId.HasValue && note.PatientId > 0)
        {
            ModelState.Remove("PatientId");
            // Re-validate manually to ensure it's correct
            var patientExists = await _context.Patients.AnyAsync(p => p.Id == note.PatientId.Value);
            if (!patientExists)
            {
                ModelState.AddModelError("PatientId", "Selected patient does not exist.");
                _logger.LogWarning("Patient {PatientId} does not exist", note.PatientId.Value);
            }
        }
        else
        {
            ModelState.AddModelError("PatientId", "Please select a patient.");
            _logger.LogWarning("PatientId validation failed: {PatientId}", note.PatientId);
        }
        
        if (note.NurseId.HasValue && note.NurseId > 0)
        {
            ModelState.Remove("NurseId");
            // Re-validate manually to ensure it's correct
            var nurseExists = await _context.Nurses.AnyAsync(n => n.Id == note.NurseId.Value);
            if (!nurseExists)
            {
                ModelState.AddModelError("NurseId", "Selected nurse does not exist.");
                _logger.LogWarning("Nurse {NurseId} does not exist", note.NurseId.Value);
            }
        }
        else
        {
            ModelState.AddModelError("NurseId", "Nurse is required. Please select a nurse or ensure your profile is linked.");
            _logger.LogWarning("NurseId validation failed after auto-assignment attempt: {NurseId}", note.NurseId);
        }

        // Validate optional fields if provided
        if (note.ProcedureId.HasValue && note.ProcedureId.Value > 0)
        {
            var procedureExists = await _context.Procedures.AnyAsync(p => p.Id == note.ProcedureId.Value);
            if (!procedureExists)
            {
                ModelState.AddModelError("ProcedureId", "Selected procedure does not exist.");
            }
        }

        if (note.AppointmentId.HasValue && note.AppointmentId.Value > 0)
        {
            var appointmentExists = await _context.Appointments.AnyAsync(a => a.Id == note.AppointmentId.Value);
            if (!appointmentExists)
            {
                ModelState.AddModelError("AppointmentId", "Selected appointment does not exist.");
            }
        }

        // Final check: if PatientId and NurseId are valid, force ModelState to be valid for these fields
        if (note.PatientId.HasValue && note.PatientId > 0 && note.NurseId.HasValue && note.NurseId > 0)
        {
            // Clear any remaining errors for these fields
            ModelState.Remove("PatientId");
            ModelState.Remove("NurseId");
            
            // Manually set these fields as valid
            if (ModelState.ContainsKey("PatientId"))
            {
                ModelState["PatientId"]!.Errors.Clear();
            }
            if (ModelState.ContainsKey("NurseId"))
            {
                ModelState["NurseId"]!.Errors.Clear();
            }
            
            _logger.LogInformation("Forced PatientId and NurseId to be valid. PatientId: {PatientId}, NurseId: {NurseId}", 
                note.PatientId.Value, note.NurseId.Value);
        }

        if (ModelState.IsValid)
        {
            try
            {
                note.CreatedDate = DateTime.Now;
                note.CreatedBy = User.Identity?.Name;
                
                _context.NursingNotes.Add(note);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Nursing note created successfully. NoteId: {NoteId}, PatientId: {PatientId}, NurseId: {NurseId}", 
                    note.Id, note.PatientId, note.NurseId);
                
                TempData["SuccessMessage"] = "Nursing note created successfully!";
                return RedirectToAction(nameof(NursingNotes));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating nursing note");
                ModelState.AddModelError("", $"An error occurred while creating the note: {ex.Message}");
            }
        }
        else
        {
            var allErrors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            _logger.LogWarning("ModelState is invalid. Errors: {Errors}", string.Join(", ", allErrors));
            
            // Log each field's errors separately for debugging
            foreach (var key in ModelState.Keys)
            {
                var errors = ModelState[key]!.Errors.Select(e => e.ErrorMessage);
                if (errors.Any())
                {
                    _logger.LogWarning("Field {Field} has errors: {Errors}", key, string.Join(", ", errors));
                }
            }
        }

        await PopulateDropDownsAsync(note.PatientId, 
            note.ProcedureId, note.AppointmentId);
        return View(note);
    }

    // GET: Nurses/RecordVitals
    public async Task<IActionResult> RecordVitals(int? patientId, int? procedureId, int? appointmentId)
    {
        await PopulateDropDownsAsync(patientId, procedureId, appointmentId);
        
        var vital = new PatientVital
        {
            PatientId = patientId,
            ProcedureId = procedureId,
            AppointmentId = appointmentId,
            RecordedDate = DateTime.Now
        };

        return View(vital);
    }

    // POST: Nurses/RecordVitals
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RecordVitals(PatientVital vital)
    {
        _logger.LogInformation("RecordVitals POST called. PatientId: {PatientId}, NurseId: {NurseId}", 
            vital.PatientId, vital.NurseId);
        
        // Try to manually read form values if model binding failed (fallback)
        if ((!vital.PatientId.HasValue || vital.PatientId <= 0) && Request.Form.ContainsKey("PatientId"))
        {
            var patientIdStr = Request.Form["PatientId"].ToString();
            if (int.TryParse(patientIdStr, out int parsedPatientId) && parsedPatientId > 0)
            {
                vital.PatientId = parsedPatientId;
                _logger.LogInformation("Manually parsed PatientId from form: {PatientId}", parsedPatientId);
                ModelState.Remove("PatientId");
            }
        }
        
        // Auto-assign current nurse if not set
        var currentNurse = await GetCurrentNurseAsync();
        if (currentNurse != null && !vital.NurseId.HasValue)
        {
            vital.NurseId = currentNurse.Id;
            _logger.LogInformation("Auto-assigned nurse {NurseId} to vital", currentNurse.Id);
        }
        
        // Clear ModelState errors for PatientId if it has a valid value
        if (vital.PatientId.HasValue && vital.PatientId > 0)
        {
            ModelState.Remove("PatientId");
            // Re-validate manually to ensure it's correct
            var patientExists = await _context.Patients.AnyAsync(p => p.Id == vital.PatientId.Value);
            if (!patientExists)
            {
                ModelState.AddModelError("PatientId", "Selected patient does not exist.");
                _logger.LogWarning("Patient {PatientId} does not exist", vital.PatientId.Value);
            }
        }
        else
        {
            ModelState.AddModelError("PatientId", "Please select a patient.");
            _logger.LogWarning("PatientId validation failed: {PatientId}", vital.PatientId);
        }
        
        // Final check: if PatientId is valid, force ModelState to be valid for this field
        if (vital.PatientId.HasValue && vital.PatientId > 0)
        {
            ModelState.Remove("PatientId");
            if (ModelState.ContainsKey("PatientId"))
            {
                ModelState["PatientId"]!.Errors.Clear();
            }
        }

        if (ModelState.IsValid)
        {
            try
            {
                vital.CreatedDate = DateTime.Now;
                vital.RecordedBy = User.Identity?.Name;
                
                _context.PatientVitals.Add(vital);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Patient vital created successfully. VitalId: {VitalId}, PatientId: {PatientId}", 
                    vital.Id, vital.PatientId);
                
                TempData["SuccessMessage"] = "Patient vitals recorded successfully!";
                return RedirectToAction(nameof(PatientVitals));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating patient vital");
                ModelState.AddModelError("", $"An error occurred while recording vitals: {ex.Message}");
            }
        }
        else
        {
            var allErrors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            _logger.LogWarning("ModelState is invalid. Errors: {Errors}", string.Join(", ", allErrors));
        }

        await PopulateDropDownsAsync(vital.PatientId, vital.ProcedureId, vital.AppointmentId);
        return View(vital);
    }

    // GET: Nurses/PatientVitals
    public async Task<IActionResult> PatientVitals(int? patientId)
    {
        var currentNurse = await GetCurrentNurseAsync();
        
        var query = _context.PatientVitals
            .Include(v => v.Patient)
            .Include(v => v.Procedure)
            .Include(v => v.Appointment)
            .Include(v => v.Nurse)
            .AsQueryable();

        // For nurses, only show vitals they recorded
        if (currentNurse != null && !User.IsInRole("Admin"))
        {
            query = query.Where(v => v.NurseId == currentNurse.Id);
        }

        if (patientId.HasValue)
        {
            query = query.Where(v => v.PatientId.HasValue && v.PatientId.Value == patientId.Value);
        }

        var vitals = await query.OrderByDescending(v => v.RecordedDate).ToListAsync();
        return View(vitals);
    }

    // GET: Nurses/UpdatePatientProgress
    public async Task<IActionResult> UpdatePatientProgress(int? patientId, int? procedureId)
    {
        if (!patientId.HasValue)
        {
            return NotFound();
        }

        var currentNurse = await GetCurrentNurseAsync();
        
        // For nurses, verify they have access to this patient
        if (currentNurse != null && !User.IsInRole("Admin"))
        {
            var hasAccess = await _context.Appointments
                .AnyAsync(a => a.NurseId == currentNurse.Id && a.PatientId == patientId.Value) ||
                await _context.Procedures
                .AnyAsync(p => p.NurseId == currentNurse.Id && p.PatientId == patientId.Value);
            
            if (!hasAccess)
            {
                TempData["ErrorMessage"] = "You do not have access to this patient's records.";
                return RedirectToAction(nameof(NursingNotes));
            }
        }

        var patient = await _patientService.GetPatientByIdAsync(patientId.Value);
        if (patient == null)
        {
            return NotFound();
        }

        var query = _context.NursingNotes
            .Where(n => n.PatientId == patientId);
        
        if (currentNurse != null && !User.IsInRole("Admin"))
        {
            query = query.Where(n => n.NurseId == currentNurse.Id);
        }
        
        if (procedureId.HasValue)
        {
            query = query.Where(n => n.ProcedureId == procedureId);
        }

        var recentNote = await query
            .OrderByDescending(n => n.NoteDate)
            .FirstOrDefaultAsync();

        ViewBag.Patient = patient;
        ViewBag.ProcedureId = procedureId;

        if (recentNote == null)
        {
            recentNote = new NursingNote
            {
                PatientId = patientId,
                ProcedureId = procedureId,
                NoteDate = DateTime.Now
            };
            
            // Auto-assign current nurse
            if (currentNurse != null)
            {
                recentNote.NurseId = currentNurse.Id;
            }
        }

        return View(recentNote);
    }

    // POST: Nurses/UpdatePatientProgress
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdatePatientProgress(NursingNote note)
    {
        var currentNurse = await GetCurrentNurseAsync();
        
        // Auto-assign current nurse if not set
        if (currentNurse != null && (!note.NurseId.HasValue || note.NurseId <= 0))
        {
            note.NurseId = currentNurse.Id;
        }

        // For nurses, verify they have access to this patient
        if (currentNurse != null && !User.IsInRole("Admin") && note.PatientId.HasValue)
        {
            var hasAccess = await _context.Appointments
                .AnyAsync(a => a.NurseId == currentNurse.Id && a.PatientId == note.PatientId.Value) ||
                await _context.Procedures
                .AnyAsync(p => p.NurseId == currentNurse.Id && p.PatientId == note.PatientId.Value);
            
            if (!hasAccess)
            {
                TempData["ErrorMessage"] = "You do not have access to this patient's records.";
                return RedirectToAction(nameof(NursingNotes));
            }
        }

        if (ModelState.IsValid)
        {
            if (note.Id == 0)
            {
                note.CreatedDate = DateTime.Now;
                note.CreatedBy = User.Identity?.Name;
                _context.NursingNotes.Add(note);
            }
            else
            {
                var existingNote = await _context.NursingNotes.FindAsync(note.Id);
                if (existingNote != null)
                {
                    // For nurses, ensure they can only update their own notes
                    if (currentNurse != null && !User.IsInRole("Admin") && 
                        existingNote.NurseId.HasValue && existingNote.NurseId.Value != currentNurse.Id)
                    {
                        TempData["ErrorMessage"] = "You can only update your own nursing notes.";
                        return RedirectToAction(nameof(NursingNotes));
                    }
                    
                    existingNote.PatientProgress = note.PatientProgress;
                    existingNote.Notes = note.Notes;
                    existingNote.MedicationsAdministered = note.MedicationsAdministered;
                    _context.NursingNotes.Update(existingNote);
                }
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Patient progress updated successfully!";
            return RedirectToAction(nameof(NursingNotes), new { patientId = note.PatientId, procedureId = note.ProcedureId });
        }

        if (note.PatientId.HasValue)
        {
            var patient = await _patientService.GetPatientByIdAsync(note.PatientId.Value);
            ViewBag.Patient = patient;
        }
        return View(note);
    }

    private async Task PopulateDropDownsAsync(int? selectedPatient = null, int? procedureId = null, int? appointmentId = null)
    {
        var currentNurse = await GetCurrentNurseAsync();
        
        // For nurses, only show assigned patients, appointments, and procedures
        IEnumerable<Patient> patients;
        IEnumerable<Procedure> procedures;
        IEnumerable<Appointment> appointments;
        
        if (currentNurse != null && !User.IsInRole("Admin"))
        {
            // Get patient IDs from assigned appointments and procedures
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
            
            patients = await _context.Patients
                .Where(p => allPatientIds.Contains(p.Id))
                .OrderBy(p => p.LastName)
                .ThenBy(p => p.FirstName)
                .ToListAsync();

            procedures = await _context.Procedures
                .Where(p => p.NurseId == currentNurse.Id)
                .OrderByDescending(p => p.ProcedureDate)
                .ToListAsync();

            appointments = await _context.Appointments
                .Where(a => a.NurseId == currentNurse.Id)
                .OrderByDescending(a => a.AppointmentDate)
                .ToListAsync();
        }
        else
        {
            // Admin sees all
            patients = await _patientService.GetAllPatientsAsync();
            procedures = await _procedureService.GetAllProceduresAsync();
            appointments = await _appointmentService.GetAllAppointmentsAsync();
        }

        // Filter procedures and appointments if specific IDs are provided
        if (procedureId.HasValue)
        {
            procedures = procedures.Where(p => p.Id == procedureId.Value).ToList();
        }
        if (appointmentId.HasValue)
        {
            appointments = appointments.Where(a => a.Id == appointmentId.Value).ToList();
        }

        var nurses = await _nurseRepository.GetAllAsync();

        ViewBag.Patients = new SelectList(patients, "Id", "FullName", selectedPatient);
        
        // Pre-select current nurse if available
        int? selectedNurseId = null;
        if (currentNurse != null)
        {
            selectedNurseId = currentNurse.Id;
        }
        ViewBag.Nurses = new SelectList(nurses.Where(n => n.IsActive), "Id", "FullName", selectedNurseId);
        
        ViewBag.Procedures = new SelectList(procedures, "Id", "ProcedureName", procedureId);
        
        // Create meaningful display text for appointments
        var appointmentItems = appointments.Select(a => new {
            Id = a.Id,
            DisplayText = $"Appointment #{a.Id} - {a.AppointmentDate:dd MMM yyyy HH:mm} - {a.Patient?.FullName ?? "N/A"}"
        }).ToList();
        ViewBag.Appointments = new SelectList(appointmentItems, "Id", "DisplayText", appointmentId);
        
        // Also provide raw collections for the view if needed
        ViewBag.AppointmentsList = appointments;
        ViewBag.CurrentNurseId = selectedNurseId;
    }
}

