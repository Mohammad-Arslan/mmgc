using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MMGC.Models;
using MMGC.Services;

namespace MMGC.Controllers;

[Authorize]
public class TransactionsController : Controller
{
    private readonly ITransactionService _transactionService;
    private readonly IPatientService _patientService;
    private readonly IAppointmentService _appointmentService;
    private readonly IProcedureService _procedureService;
    private readonly ILabTestService _labTestService;
    private readonly ILogger<TransactionsController> _logger;

    public TransactionsController(
        ITransactionService transactionService,
        IPatientService patientService,
        IAppointmentService appointmentService,
        IProcedureService procedureService,
        ILabTestService labTestService,
        ILogger<TransactionsController> logger)
    {
        _transactionService = transactionService;
        _patientService = patientService;
        _appointmentService = appointmentService;
        _procedureService = procedureService;
        _labTestService = labTestService;
        _logger = logger;
    }

    // Helper to populate dropdowns
    private async Task PopulateDropDownsAsync(
        object? selectedPatient = null, 
        object? selectedAppointment = null, 
        object? selectedProcedure = null, 
        object? selectedLabTest = null)
    {
        var patients = await _patientService.GetAllPatientsAsync();
        var appointments = await _appointmentService.GetAllAppointmentsAsync();
        var procedures = await _procedureService.GetAllProceduresAsync();
        var labTests = await _labTestService.GetAllLabTestsAsync();

        ViewBag.Patients = new SelectList(patients, "Id", "FullName", selectedPatient);
        ViewBag.Appointments = new SelectList(appointments, "Id", "Id", selectedAppointment);
        ViewBag.Procedures = new SelectList(procedures, "Id", "ProcedureName", selectedProcedure);
        ViewBag.LabTests = new SelectList(labTests, "Id", "TestName", selectedLabTest);
        
        // Also provide raw collections for Edit view
        ViewBag.PatientsList = patients;
        ViewBag.AppointmentsList = appointments;
        ViewBag.ProceduresList = procedures;
        ViewBag.LabTestsList = labTests;
    }

    // GET: Transactions
    public async Task<IActionResult> Index()
    {
        var transactions = await _transactionService.GetAllTransactionsAsync();
        return View(transactions);
    }

    // GET: Transactions/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var transaction = await _transactionService.GetTransactionByIdAsync(id.Value);
        if (transaction == null)
        {
            return NotFound();
        }

        return View(transaction);
    }

    // GET: Transactions/Create
    public async Task<IActionResult> Create()
    {
        await PopulateDropDownsAsync();
        var model = new Transaction
        {
            TransactionDate = DateTime.Now
        };
        return View(model);
    }

    // POST: Transactions/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Transaction transaction)
    {
        // Clear navigation property errors
        ModelState.Remove(nameof(Transaction.Patient));
        ModelState.Remove(nameof(Transaction.Appointment));
        ModelState.Remove(nameof(Transaction.Procedure));
        ModelState.Remove(nameof(Transaction.LabTest));

        if (!ModelState.IsValid)
        {
            await PopulateDropDownsAsync(transaction.PatientId, transaction.AppointmentId, transaction.ProcedureId, transaction.LabTestId);
            return View(transaction);
        }

        // Server-side existence checks
        var patient = await _patientService.GetPatientByIdAsync(transaction.PatientId);
        if (patient == null)
        {
            ModelState.AddModelError(nameof(Transaction.PatientId), "Selected patient does not exist.");
            await PopulateDropDownsAsync(transaction.PatientId, transaction.AppointmentId, transaction.ProcedureId, transaction.LabTestId);
            return View(transaction);
        }

        try
        {
            transaction.Patient = null!;
            transaction.Appointment = null;
            transaction.Procedure = null;
            transaction.LabTest = null;

            transaction.CreatedBy = User.Identity?.Name;
            transaction.CreatedDate = DateTime.Now;

            await _transactionService.CreateTransactionAsync(transaction);

            TempData["SuccessMessage"] = "Transaction created successfully!";
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error creating transaction");
            ModelState.AddModelError("", "A database error occurred while creating the transaction.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating transaction");
            ModelState.AddModelError("", $"An error occurred: {ex.Message}");
        }

        await PopulateDropDownsAsync(transaction.PatientId, transaction.AppointmentId, transaction.ProcedureId, transaction.LabTestId);
        return View(transaction);
    }

    // GET: Transactions/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var transaction = await _transactionService.GetTransactionByIdAsync(id.Value);
        if (transaction == null)
        {
            return NotFound();
        }

        await PopulateDropDownsAsync(transaction.PatientId, transaction.AppointmentId, transaction.ProcedureId, transaction.LabTestId);
        return View(transaction);
    }

    // POST: Transactions/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Transaction transaction)
    {
        if (id != transaction.Id)
        {
            return NotFound();
        }

        // Clear navigation property errors
        ModelState.Remove(nameof(Transaction.Patient));
        ModelState.Remove(nameof(Transaction.Appointment));
        ModelState.Remove(nameof(Transaction.Procedure));
        ModelState.Remove(nameof(Transaction.LabTest));

        if (!ModelState.IsValid)
        {
            await PopulateDropDownsAsync(transaction.PatientId, transaction.AppointmentId, transaction.ProcedureId, transaction.LabTestId);
            return View(transaction);
        }

        // Server-side existence checks
        var patient = await _patientService.GetPatientByIdAsync(transaction.PatientId);
        if (patient == null)
        {
            ModelState.AddModelError(nameof(Transaction.PatientId), "Selected patient does not exist.");
            await PopulateDropDownsAsync(transaction.PatientId, transaction.AppointmentId, transaction.ProcedureId, transaction.LabTestId);
            return View(transaction);
        }

        try
        {
            transaction.Patient = null!;
            transaction.Appointment = null;
            transaction.Procedure = null;
            transaction.LabTest = null;

            await _transactionService.UpdateTransactionAsync(transaction);

            TempData["SuccessMessage"] = "Transaction updated successfully!";
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await TransactionExists(transaction.Id))
            {
                return NotFound();
            }
            ModelState.AddModelError("", "Another user modified this record. Please reload and try again.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating transaction {TransactionId}", id);
            ModelState.AddModelError("", "An error occurred while updating the transaction.");
        }

        await PopulateDropDownsAsync(transaction.PatientId, transaction.AppointmentId, transaction.ProcedureId, transaction.LabTestId);
        return View(transaction);
    }

    // GET: Transactions/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var transaction = await _transactionService.GetTransactionByIdAsync(id.Value);
        if (transaction == null)
        {
            return NotFound();
        }

        return View(transaction);
    }

    // POST: Transactions/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        try
        {
            await _transactionService.DeleteTransactionAsync(id);
            TempData["SuccessMessage"] = "Transaction deleted successfully!";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting transaction {TransactionId}", id);
            TempData["ErrorMessage"] = "An error occurred while deleting the transaction.";
            return RedirectToAction(nameof(Index));
        }
    }

    // POST: Transactions/GenerateInvoice/5
    [HttpPost]
    public async Task<IActionResult> GenerateInvoice(int id)
    {
        try
        {
            var invoicePath = await _transactionService.GenerateInvoiceAsync(id);
            _logger.LogInformation("Invoice generated for transaction {TransactionId}: {InvoicePath}", id, invoicePath);
            return Json(new { success = true, invoicePath });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating invoice for transaction {TransactionId}", id);
            return Json(new { success = false, message = ex.Message });
        }
    }

    // GET: Transactions/DownloadInvoice/5
    public async Task<IActionResult> DownloadInvoice(int id)
    {
        try
        {
            var transaction = await _transactionService.GetTransactionByIdAsync(id);
            if (transaction == null)
            {
                return NotFound();
            }

            if (string.IsNullOrEmpty(transaction.InvoicePath) || !transaction.InvoiceGenerated)
            {
                TempData["ErrorMessage"] = "Invoice has not been generated for this transaction.";
                return RedirectToAction(nameof(Details), new { id });
            }

            // Remove leading slash if present
            var filePath = transaction.InvoicePath.TrimStart('/');
            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", filePath);

            if (!System.IO.File.Exists(fullPath))
            {
                TempData["ErrorMessage"] = "Invoice file not found. Please regenerate the invoice.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var fileName = Path.GetFileName(fullPath);
            var fileBytes = await System.IO.File.ReadAllBytesAsync(fullPath);
            
            // Determine content type based on file extension
            var contentType = fileName.EndsWith(".html", StringComparison.OrdinalIgnoreCase) 
                ? "text/html" 
                : "application/pdf";
            
            // For HTML files, return as HTML so browser can open it
            if (fileName.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
            {
                return File(fileBytes, contentType);
            }
            
            // For PDF files, force download
            return File(fileBytes, contentType, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading invoice for transaction {TransactionId}", id);
            TempData["ErrorMessage"] = "An error occurred while downloading the invoice.";
            return RedirectToAction(nameof(Details), new { id });
        }
    }

    private async Task<bool> TransactionExists(int id)
    {
        var transaction = await _transactionService.GetTransactionByIdAsync(id);
        return transaction != null;
    }
}
