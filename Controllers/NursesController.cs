using Microsoft.AspNetCore.Authorization;
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
    private readonly ILogger<NursesController> _logger;

    public NursesController(
        ApplicationDbContext context,
        IRepository<Nurse> nurseRepository,
        IPatientService patientService,
        IProcedureService procedureService,
        IAppointmentService appointmentService,
        ILogger<NursesController> logger)
    {
        _context = context;
        _nurseRepository = nurseRepository;
        _patientService = patientService;
        _procedureService = procedureService;
        _appointmentService = appointmentService;
        _logger = logger;
    }

    // GET: Nurses/Dashboard
    public async Task<IActionResult> Dashboard()
    {
        var nurses = await _nurseRepository.GetAllAsync();
        var activeNurses = nurses.Where(n => n.IsActive).Count();
        var totalProcedures = await _context.Procedures.CountAsync();
        var todayNotes = await _context.NursingNotes
            .Where(n => n.NoteDate.Date == DateTime.Today)
            .CountAsync();

        ViewBag.ActiveNurses = activeNurses;
        ViewBag.TotalProcedures = totalProcedures;
        ViewBag.TodayNotes = todayNotes;

        return View();
    }

    // GET: Nurses/NursingNotes
    public async Task<IActionResult> NursingNotes(int? patientId, int? procedureId)
    {
        var query = _context.NursingNotes
            .Include(n => n.Patient)
            .Include(n => n.Procedure)
            .Include(n => n.Appointment)
            .Include(n => n.Nurse)
            .AsQueryable();

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
        
        var note = new NursingNote
        {
            PatientId = patientId ?? 0,
            ProcedureId = procedureId,
            AppointmentId = appointmentId,
            NoteDate = DateTime.Now
        };

        return View(note);
    }

    // POST: Nurses/CreateNursingNote
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateNursingNote(NursingNote note)
    {
        if (ModelState.IsValid)
        {
            note.CreatedDate = DateTime.Now;
            note.CreatedBy = User.Identity?.Name;
            
            _context.NursingNotes.Add(note);
            await _context.SaveChangesAsync();
            
            TempData["SuccessMessage"] = "Nursing note created successfully!";
            return RedirectToAction(nameof(NursingNotes));
        }

        await PopulateDropDownsAsync(note.PatientId, note.ProcedureId, note.AppointmentId);
        return View(note);
    }

    // GET: Nurses/RecordVitals
    public async Task<IActionResult> RecordVitals(int? patientId, int? procedureId, int? appointmentId)
    {
        await PopulateDropDownsAsync(patientId, procedureId, appointmentId);
        
        var vital = new PatientVital
        {
            PatientId = patientId ?? 0,
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
        if (ModelState.IsValid)
        {
            vital.CreatedDate = DateTime.Now;
            vital.RecordedBy = User.Identity?.Name;
            
            _context.PatientVitals.Add(vital);
            await _context.SaveChangesAsync();
            
            TempData["SuccessMessage"] = "Patient vitals recorded successfully!";
            return RedirectToAction(nameof(PatientVitals));
        }

        await PopulateDropDownsAsync(vital.PatientId, vital.ProcedureId, vital.AppointmentId);
        return View(vital);
    }

    // GET: Nurses/PatientVitals
    public async Task<IActionResult> PatientVitals(int? patientId)
    {
        var query = _context.PatientVitals
            .Include(v => v.Patient)
            .Include(v => v.Procedure)
            .Include(v => v.Appointment)
            .Include(v => v.Nurse)
            .AsQueryable();

        if (patientId.HasValue)
        {
            query = query.Where(v => v.PatientId == patientId.Value);
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

        var patient = await _patientService.GetPatientByIdAsync(patientId.Value);
        if (patient == null)
        {
            return NotFound();
        }

        var recentNote = await _context.NursingNotes
            .Where(n => n.PatientId == patientId && n.ProcedureId == procedureId)
            .OrderByDescending(n => n.NoteDate)
            .FirstOrDefaultAsync();

        ViewBag.Patient = patient;
        ViewBag.ProcedureId = procedureId;

        if (recentNote == null)
        {
            recentNote = new NursingNote
            {
                PatientId = patientId.Value,
                ProcedureId = procedureId,
                NoteDate = DateTime.Now
            };
        }

        return View(recentNote);
    }

    // POST: Nurses/UpdatePatientProgress
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdatePatientProgress(NursingNote note)
    {
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

        var patient = await _patientService.GetPatientByIdAsync(note.PatientId);
        ViewBag.Patient = patient;
        return View(note);
    }

    private async Task PopulateDropDownsAsync(int? selectedPatient = null, int? procedureId = null, int? appointmentId = null)
    {
        var patients = await _patientService.GetAllPatientsAsync();
        var nurses = await _nurseRepository.GetAllAsync();
        var allProcedures = await _procedureService.GetAllProceduresAsync();
        var allAppointments = await _appointmentService.GetAllAppointmentsAsync();
        
        var procedures = procedureId.HasValue 
            ? allProcedures.Where(p => p.Id == procedureId.Value).ToList()
            : allProcedures.ToList();
        var appointments = appointmentId.HasValue 
            ? allAppointments.Where(a => a.Id == appointmentId.Value).ToList()
            : allAppointments.ToList();

        ViewBag.Patients = new SelectList(patients, "Id", "FullName", selectedPatient);
        ViewBag.Nurses = new SelectList(nurses.Where(n => n.IsActive), "Id", "FullName");
        ViewBag.Procedures = new SelectList(procedures, "Id", "ProcedureName", procedureId);
        ViewBag.Appointments = new SelectList(appointments, "Id", "Id", appointmentId);
    }
}

