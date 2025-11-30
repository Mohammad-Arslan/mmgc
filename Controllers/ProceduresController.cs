using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MMGC.Models;
using MMGC.Services;
using MMGC.Repositories;

namespace MMGC.Controllers;

[Authorize]
public class ProceduresController : Controller
{
    private readonly IProcedureService _procedureService;
    private readonly IPatientService _patientService;
    private readonly IDoctorService _doctorService;
    private readonly IRepository<Nurse> _nurseRepository;

    public ProceduresController(
        IProcedureService procedureService,
        IPatientService patientService,
        IDoctorService doctorService,
        IRepository<Nurse> nurseRepository)
    {
        _procedureService = procedureService;
        _patientService = patientService;
        _doctorService = doctorService;
        _nurseRepository = nurseRepository;
    }

    // GET: Procedures
    public async Task<IActionResult> Index()
    {
        var procedures = await _procedureService.GetAllProceduresAsync();
        return View(procedures);
    }

    // GET: Procedures/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var procedure = await _procedureService.GetProcedureByIdAsync(id.Value);
        if (procedure == null)
        {
            return NotFound();
        }

        return View(procedure);
    }

    // GET: Procedures/Create
    public async Task<IActionResult> Create()
    {
        ViewBag.Patients = await _patientService.GetAllPatientsAsync();
        ViewBag.Doctors = await _doctorService.GetAllDoctorsAsync();
        ViewBag.Nurses = await _nurseRepository.GetAllAsync();
        return View();
    }

    // POST: Procedures/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("PatientId,DoctorId,NurseId,ProcedureName,ProcedureType,ProcedureDate,TreatmentNotes,Prescription,ProcedureFee,Status")] Procedure procedure)
    {
        if (ModelState.IsValid)
        {
            procedure.CreatedBy = User.Identity?.Name;
            await _procedureService.CreateProcedureAsync(procedure);
            return RedirectToAction(nameof(Index));
        }

        ViewBag.Patients = await _patientService.GetAllPatientsAsync();
        ViewBag.Doctors = await _doctorService.GetAllDoctorsAsync();
        ViewBag.Nurses = await _nurseRepository.GetAllAsync();
        return View(procedure);
    }

    // GET: Procedures/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var procedure = await _procedureService.GetProcedureByIdAsync(id.Value);
        if (procedure == null)
        {
            return NotFound();
        }

        ViewBag.Patients = await _patientService.GetAllPatientsAsync();
        ViewBag.Doctors = await _doctorService.GetAllDoctorsAsync();
        ViewBag.Nurses = await _nurseRepository.GetAllAsync();
        return View(procedure);
    }

    // POST: Procedures/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,PatientId,DoctorId,NurseId,ProcedureName,ProcedureType,ProcedureDate,TreatmentNotes,Prescription,ProcedureFee,Status")] Procedure procedure)
    {
        if (id != procedure.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                await _procedureService.UpdateProcedureAsync(procedure);
            }
            catch
            {
                if (!await ProcedureExists(procedure.Id))
                {
                    return NotFound();
                }
                throw;
            }
            return RedirectToAction(nameof(Index));
        }

        ViewBag.Patients = await _patientService.GetAllPatientsAsync();
        ViewBag.Doctors = await _doctorService.GetAllDoctorsAsync();
        ViewBag.Nurses = await _nurseRepository.GetAllAsync();
        return View(procedure);
    }

    // GET: Procedures/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var procedure = await _procedureService.GetProcedureByIdAsync(id.Value);
        if (procedure == null)
        {
            return NotFound();
        }

        return View(procedure);
    }

    // POST: Procedures/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        await _procedureService.DeleteProcedureAsync(id);
        return RedirectToAction(nameof(Index));
    }

    private async Task<bool> ProcedureExists(int id)
    {
        var procedure = await _procedureService.GetProcedureByIdAsync(id);
        return procedure != null;
    }
}
