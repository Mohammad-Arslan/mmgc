using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MMGC.Models;
using MMGC.Services;

namespace MMGC.Controllers;

[Authorize]
public class PatientsController : Controller
{
    private readonly IPatientService _patientService;

    public PatientsController(IPatientService patientService)
    {
        _patientService = patientService;
    }

    // GET: Patients
    public async Task<IActionResult> Index()
    {
        var patients = await _patientService.GetAllPatientsAsync();
        return View(patients);
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
        if (ModelState.IsValid)
        {
            await _patientService.CreatePatientAsync(patient);
            return RedirectToAction(nameof(Index));
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
            }
            catch
            {
                if (!await PatientExists(patient.Id))
                {
                    return NotFound();
                }
                throw;
            }
            return RedirectToAction(nameof(Index));
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
        await _patientService.DeletePatientAsync(id);
        return RedirectToAction(nameof(Index));
    }

    private async Task<bool> PatientExists(int id)
    {
        var patient = await _patientService.GetPatientByIdAsync(id);
        return patient != null;
    }
}
