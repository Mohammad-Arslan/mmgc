using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MMGC.Data;
using MMGC.Shared.Interfaces;

namespace MMGC.Controllers;

/// <summary>
/// Controller for authenticated patient document downloads (invoices, lab reports, prescriptions).
/// All endpoints require authentication and enforce row-level security.
/// </summary>
[Authorize(Policy = "PatientOnly")]
[Route("patient/documents/[action]")]
public class DocumentDownloadsController : Controller
{
    private readonly IDocumentDownloadService _documentDownloadService;
    private readonly IPdfService _pdfService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DocumentDownloadsController> _logger;

    public DocumentDownloadsController(
        IDocumentDownloadService documentDownloadService,
        IPdfService pdfService,
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext context,
        ILogger<DocumentDownloadsController> logger)
    {
        _documentDownloadService = documentDownloadService;
        _pdfService = pdfService;
        _userManager = userManager;
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Gets patient's ID from user context.
    /// </summary>
    private async Task<int?> GetPatientIdAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return null;

        var patient = await _context.Patients.FirstOrDefaultAsync(p => p.UserId == user.Id);
        return patient?.Id;
    }

    /// <summary>
    /// List of invoices for authenticated patient.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Invoices(int page = 1, CancellationToken cancellationToken = default)
    {
        try
        {
            var patientId = await GetPatientIdAsync();
            if (!patientId.HasValue)
                return Unauthorized("Patient not found");

            const int pageSize = 10;
            var (invoices, totalCount) = await _documentDownloadService.GetPatientInvoicesAsync(
                patientId.Value, page, pageSize, cancellationToken);

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalCount / pageSize);
            ViewBag.TotalCount = totalCount;

            return View(invoices);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading invoices");
            return BadRequest("Unable to load invoices");
        }
    }

    /// <summary>
    /// Download invoice. Serves the actual generated file (HTML/PDF) when available, same as staff view.
    /// Enforces row-level security - patient can only download their own invoices.
    /// </summary>
    [HttpGet("{invoiceId:int}")]
    public async Task<IActionResult> DownloadInvoice(int invoiceId, CancellationToken cancellationToken = default)
    {
        try
        {
            var patientId = await GetPatientIdAsync();
            if (!patientId.HasValue)
                return Unauthorized("Patient not found");

            // Verify invoice ownership
            var invoice = await _documentDownloadService.GetInvoiceAsync(invoiceId, patientId.Value, cancellationToken);
            if (invoice == null)
                return NotFound("Invoice not found or access denied");

            // Serve the actual generated file (same logic as TransactionsController) when available
            if (!string.IsNullOrEmpty(invoice.InvoicePath))
            {
                var relativePath = invoice.InvoicePath.TrimStart('/');
                var physicalPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", relativePath);

                if (System.IO.File.Exists(physicalPath))
                {
                    var contentType = GetContentTypeForExtension(Path.GetExtension(physicalPath));
                    var downloadFileName = Path.GetFileName(physicalPath);
                    return PhysicalFile(physicalPath, contentType, downloadFileName);
                }
            }

            // Fallback: generate PDF from metadata (e.g. if file was deleted)
            var pdfBytes = await _pdfService.GenerateInvoicePdfAsync(invoiceId, cancellationToken);
            var fileName = $"Invoice-{invoiceId:D6}-{DateTime.Now:yyyy-MM-dd}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading invoice {InvoiceId}", invoiceId);
            return StatusCode(500, "Unable to download invoice");
        }
    }

    /// <summary>
    /// List of lab reports for authenticated patient.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> LabReports(int page = 1, CancellationToken cancellationToken = default)
    {
        try
        {
            var patientId = await GetPatientIdAsync();
            if (!patientId.HasValue)
                return Unauthorized("Patient not found");

            const int pageSize = 10;
            var (reports, totalCount) = await _documentDownloadService.GetPatientLabReportsAsync(
                patientId.Value, page, pageSize, cancellationToken);

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalCount / pageSize);
            ViewBag.TotalCount = totalCount;

            return View(reports);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading lab reports");
            return BadRequest("Unable to load lab reports");
        }
    }

    /// <summary>
    /// Download lab report. Serves the actual uploaded file (same as doctors see).
    /// Only approved reports can be downloaded.
    /// </summary>
    [HttpGet("{labTestId:int}")]
    public async Task<IActionResult> DownloadLabReport(int labTestId, CancellationToken cancellationToken = default)
    {
        try
        {
            var patientId = await GetPatientIdAsync();
            if (!patientId.HasValue)
                return Unauthorized("Patient not found");

            // Verify report ownership and approval
            var report = await _documentDownloadService.GetLabReportAsync(labTestId, patientId.Value, cancellationToken);
            if (report == null)
                return NotFound("Lab report not found or access denied");

            if (!report.CanDownload)
                return BadRequest("Report is not yet approved for download");

            // Serve the actual uploaded file (same logic as doctors) - not generated PDF
            if (!string.IsNullOrEmpty(report.ReportFilePath))
            {
                var relativePath = report.ReportFilePath.TrimStart('/');
                var physicalPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", relativePath);

                if (System.IO.File.Exists(physicalPath))
                {
                    var contentType = GetContentTypeForExtension(Path.GetExtension(physicalPath));
                    var downloadFileName = $"LabReport-{labTestId:D6}-{DateTime.Now:yyyy-MM-dd}{Path.GetExtension(physicalPath)}";
                    return PhysicalFile(physicalPath, contentType, downloadFileName);
                }
            }

            // Fallback: generate PDF from metadata (e.g. if file was deleted)
            var pdfBytes = await _pdfService.GenerateLabReportPdfAsync(labTestId, cancellationToken);
            var fileName = $"LabReport-{labTestId:D6}-{DateTime.Now:yyyy-MM-dd}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading lab report {LabTestId}", labTestId);
            return StatusCode(500, "Unable to download lab report");
        }
    }

    private static string GetContentTypeForExtension(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".pdf" => "application/pdf",
            ".html" or ".htm" => "text/html",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            _ => "application/octet-stream"
        };
    }

    /// <summary>
    /// List of prescriptions for authenticated patient.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Prescriptions(int page = 1, CancellationToken cancellationToken = default)
    {
        try
        {
            var patientId = await GetPatientIdAsync();
            if (!patientId.HasValue)
                return Unauthorized("Patient not found");

            const int pageSize = 10;
            var (prescriptions, totalCount) = await _documentDownloadService.GetPatientPrescriptionsAsync(
                patientId.Value, page, pageSize, cancellationToken);

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalCount / pageSize);
            ViewBag.TotalCount = totalCount;

            return View(prescriptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading prescriptions");
            return BadRequest("Unable to load prescriptions");
        }
    }

    /// <summary>
    /// Download prescription as PDF.
    /// </summary>
    [HttpGet("{prescriptionId:int}")]
    public async Task<IActionResult> DownloadPrescription(int prescriptionId, CancellationToken cancellationToken = default)
    {
        try
        {
            var patientId = await GetPatientIdAsync();
            if (!patientId.HasValue)
                return Unauthorized("Patient not found");

            // Verify prescription ownership
            var prescription = await _documentDownloadService.GetPrescriptionAsync(prescriptionId, patientId.Value, cancellationToken);
            if (prescription == null)
                return NotFound("Prescription not found or access denied");

            // Generate PDF
            var pdfBytes = await _pdfService.GeneratePrescriptionPdfAsync(prescriptionId, cancellationToken);

            var fileName = $"Prescription-{prescriptionId:D6}-{DateTime.Now:yyyy-MM-dd}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading prescription {PrescriptionId}", prescriptionId);
            return StatusCode(500, "Unable to download prescription");
        }
    }
}
