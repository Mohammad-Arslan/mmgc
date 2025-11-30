using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

    public TransactionsController(
        ITransactionService transactionService,
        IPatientService patientService,
        IAppointmentService appointmentService,
        IProcedureService procedureService,
        ILabTestService labTestService)
    {
        _transactionService = transactionService;
        _patientService = patientService;
        _appointmentService = appointmentService;
        _procedureService = procedureService;
        _labTestService = labTestService;
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
        ViewBag.Patients = await _patientService.GetAllPatientsAsync();
        ViewBag.Appointments = await _appointmentService.GetAllAppointmentsAsync();
        ViewBag.Procedures = await _procedureService.GetAllProceduresAsync();
        ViewBag.LabTests = await _labTestService.GetAllLabTestsAsync();
        return View();
    }

    // POST: Transactions/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("PatientId,TransactionType,AppointmentId,ProcedureId,LabTestId,Description,Amount,PaymentMode,ReferenceNumber,Status")] Transaction transaction)
    {
        if (ModelState.IsValid)
        {
            transaction.CreatedBy = User.Identity?.Name;
            await _transactionService.CreateTransactionAsync(transaction);
            return RedirectToAction(nameof(Index));
        }

        ViewBag.Patients = await _patientService.GetAllPatientsAsync();
        ViewBag.Appointments = await _appointmentService.GetAllAppointmentsAsync();
        ViewBag.Procedures = await _procedureService.GetAllProceduresAsync();
        ViewBag.LabTests = await _labTestService.GetAllLabTestsAsync();
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

        ViewBag.Patients = await _patientService.GetAllPatientsAsync();
        ViewBag.Appointments = await _appointmentService.GetAllAppointmentsAsync();
        ViewBag.Procedures = await _procedureService.GetAllProceduresAsync();
        ViewBag.LabTests = await _labTestService.GetAllLabTestsAsync();
        return View(transaction);
    }

    // POST: Transactions/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,PatientId,TransactionType,AppointmentId,ProcedureId,LabTestId,Description,Amount,PaymentMode,ReferenceNumber,Status")] Transaction transaction)
    {
        if (id != transaction.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                await _transactionService.UpdateTransactionAsync(transaction);
            }
            catch
            {
                if (!await TransactionExists(transaction.Id))
                {
                    return NotFound();
                }
                throw;
            }
            return RedirectToAction(nameof(Index));
        }

        ViewBag.Patients = await _patientService.GetAllPatientsAsync();
        ViewBag.Appointments = await _appointmentService.GetAllAppointmentsAsync();
        ViewBag.Procedures = await _procedureService.GetAllProceduresAsync();
        ViewBag.LabTests = await _labTestService.GetAllLabTestsAsync();
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
        await _transactionService.DeleteTransactionAsync(id);
        return RedirectToAction(nameof(Index));
    }

    // POST: Transactions/GenerateInvoice/5
    [HttpPost]
    public async Task<IActionResult> GenerateInvoice(int id)
    {
        try
        {
            var invoicePath = await _transactionService.GenerateInvoiceAsync(id);
            return Json(new { success = true, invoicePath });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    private async Task<bool> TransactionExists(int id)
    {
        var transaction = await _transactionService.GetTransactionByIdAsync(id);
        return transaction != null;
    }
}
