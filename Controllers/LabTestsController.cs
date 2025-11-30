using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MMGC.Models;
using MMGC.Services;
using MMGC.Repositories;

namespace MMGC.Controllers;

[Authorize]
public class LabTestsController : Controller
{
    private readonly ILabTestService _labTestService;
    private readonly IPatientService _patientService;
    private readonly IProcedureService _procedureService;

    public LabTestsController(
        ILabTestService labTestService,
        IPatientService patientService,
        IProcedureService procedureService)
    {
        _labTestService = labTestService;
        _patientService = patientService;
        _procedureService = procedureService;
    }

    // GET: LabTests
    public async Task<IActionResult> Index()
    {
        var labTests = await _labTestService.GetAllLabTestsAsync();
        return View(labTests);
    }

    // GET: LabTests/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var labTest = await _labTestService.GetLabTestByIdAsync(id.Value);
        if (labTest == null)
        {
            return NotFound();
        }

        return View(labTest);
    }

    // GET: LabTests/Create
    public async Task<IActionResult> Create()
    {
        ViewBag.Patients = await _patientService.GetAllPatientsAsync();
        ViewBag.Categories = await _labTestService.GetAllCategoriesAsync();
        ViewBag.Procedures = await _procedureService.GetAllProceduresAsync();
        return View();
    }

    // POST: LabTests/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("PatientId,LabTestCategoryId,ProcedureId,TestName,TestDate,TestFee,AssignedToUserId")] LabTest labTest)
    {
        if (ModelState.IsValid)
        {
            labTest.CreatedBy = User.Identity?.Name;
            labTest.Status = "Pending";
            await _labTestService.CreateLabTestAsync(labTest);
            return RedirectToAction(nameof(Index));
        }

        ViewBag.Patients = await _patientService.GetAllPatientsAsync();
        ViewBag.Categories = await _labTestService.GetAllCategoriesAsync();
        ViewBag.Procedures = await _procedureService.GetAllProceduresAsync();
        return View(labTest);
    }

    // GET: LabTests/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var labTest = await _labTestService.GetLabTestByIdAsync(id.Value);
        if (labTest == null)
        {
            return NotFound();
        }

        ViewBag.Patients = await _patientService.GetAllPatientsAsync();
        ViewBag.Categories = await _labTestService.GetAllCategoriesAsync();
        ViewBag.Procedures = await _procedureService.GetAllProceduresAsync();
        return View(labTest);
    }

    // POST: LabTests/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,PatientId,LabTestCategoryId,ProcedureId,TestName,TestDate,Status,TestFee,AssignedToUserId,ReportNotes")] LabTest labTest)
    {
        if (id != labTest.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                await _labTestService.UpdateLabTestAsync(labTest);
            }
            catch
            {
                if (!await LabTestExists(labTest.Id))
                {
                    return NotFound();
                }
                throw;
            }
            return RedirectToAction(nameof(Index));
        }

        ViewBag.Patients = await _patientService.GetAllPatientsAsync();
        ViewBag.Categories = await _labTestService.GetAllCategoriesAsync();
        ViewBag.Procedures = await _procedureService.GetAllProceduresAsync();
        return View(labTest);
    }

    // GET: LabTests/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var labTest = await _labTestService.GetLabTestByIdAsync(id.Value);
        if (labTest == null)
        {
            return NotFound();
        }

        return View(labTest);
    }

    // POST: LabTests/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        await _labTestService.DeleteLabTestAsync(id);
        return RedirectToAction(nameof(Index));
    }

    // GET: LabTests/UploadReport/5
    public async Task<IActionResult> UploadReport(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var labTest = await _labTestService.GetLabTestByIdAsync(id.Value);
        if (labTest == null)
        {
            return NotFound();
        }

        return View(labTest);
    }

    // POST: LabTests/UploadReport/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadReport(int id, IFormFile reportFile, string? notes)
    {
        if (reportFile == null || reportFile.Length == 0)
        {
            ModelState.AddModelError("", "Please select a file to upload.");
            var labTest = await _labTestService.GetLabTestByIdAsync(id);
            return View(labTest);
        }

        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "labreports");
        if (!Directory.Exists(uploadsFolder))
        {
            Directory.CreateDirectory(uploadsFolder);
        }

        var fileName = $"{id}_{DateTime.Now:yyyyMMddHHmmss}_{reportFile.FileName}";
        var filePath = Path.Combine(uploadsFolder, fileName);
        var relativePath = $"/uploads/labreports/{fileName}";

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await reportFile.CopyToAsync(stream);
        }

        await _labTestService.UploadReportAsync(id, relativePath, notes);
        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task<bool> LabTestExists(int id)
    {
        var labTest = await _labTestService.GetLabTestByIdAsync(id);
        return labTest != null;
    }
}
