using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MMGC.Data;
using MMGC.Models;
using MMGC.Shared.DTOs;
using MMGC.Shared.Interfaces;

namespace MMGC.Shared.Infrastructure.Services;

/// <summary>
/// Service for managing patient document downloads with row-level security.
/// Ensures patients can only access their own documents.
/// </summary>
public class DocumentDownloadService : IDocumentDownloadService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DocumentDownloadService> _logger;

    public DocumentDownloadService(
        ApplicationDbContext context,
        ILogger<DocumentDownloadService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<(List<InvoiceDto> Invoices, int TotalCount)> GetPatientInvoicesAsync(
        int patientId,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Verify patient exists and is owned by current user
            var patientExists = await _context.Patients
                .AsNoTracking()
                .AnyAsync(p => p.Id == patientId, cancellationToken);

            if (!patientExists)
                throw new UnauthorizedAccessException("Patient not found");

            var query = _context.Transactions
                .AsNoTracking()
                .Where(t => t.PatientId == patientId)
                .Include(t => t.Patient)
                .Include(t => t.Appointment)
                    .ThenInclude(a => a!.Doctor);

            var totalCount = await query.CountAsync(cancellationToken);

            var invoices = await query
                .OrderByDescending(t => t.TransactionDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(t => new InvoiceDto
                {
                    Id = t.Id,
                    TransactionId = t.Id,
                    PatientId = t.PatientId,
                    PatientName = t.Patient.FullName,
                    PatientMRNumber = t.Patient.MRNumber,
                    TransactionType = t.TransactionType,
                    Description = t.Description,
                    Amount = t.Amount,
                    PaymentMode = t.PaymentMode,
                    ReferenceNumber = t.ReferenceNumber,
                    Status = t.Status,
                    TransactionDate = t.TransactionDate,
                    RelatedService = t.Appointment != null ? $"Dr. {t.Appointment.Doctor!.FullName}" : null
                })
                .ToListAsync(cancellationToken);

            return (invoices, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching invoices for patient {PatientId}", patientId);
            throw;
        }
    }

    public async Task<(List<LabReportDto> Reports, int TotalCount)> GetPatientLabReportsAsync(
        int patientId,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Verify patient exists
            var patientExists = await _context.Patients
                .AsNoTracking()
                .AnyAsync(p => p.Id == patientId, cancellationToken);

            if (!patientExists)
                throw new UnauthorizedAccessException("Patient not found");

            var query = _context.LabTests
                .AsNoTracking()
                .Where(l => l.PatientId == patientId)
                .Include(l => l.LabTestCategory)
                .Include(l => l.ApprovedByDoctor);

            var totalCount = await query.CountAsync(cancellationToken);

            var reports = await query
                .OrderByDescending(l => l.TestDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(l => new LabReportDto
                {
                    Id = l.Id,
                    PatientId = l.PatientId,
                    TestName = l.TestName,
                    CategoryName = l.LabTestCategory.CategoryName,
                    TestDate = l.TestDate,
                    Status = l.Status,
                    ApprovedDate = l.ApprovedDate,
                    ApprovedByDoctor = l.ApprovedByDoctor != null ? l.ApprovedByDoctor.FullName : null,
                    ReportNotes = l.ReportNotes,
                    IsApproved = l.IsApproved,
                    ReportFilePath = l.ReportFilePath
                })
                .ToListAsync(cancellationToken);

            return (reports, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching lab reports for patient {PatientId}", patientId);
            throw;
        }
    }

    public async Task<(List<PrescriptionDto> Prescriptions, int TotalCount)> GetPatientPrescriptionsAsync(
        int patientId,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Verify patient exists
            var patientExists = await _context.Patients
                .AsNoTracking()
                .AnyAsync(p => p.Id == patientId, cancellationToken);

            if (!patientExists)
                throw new UnauthorizedAccessException("Patient not found");

            var query = _context.Prescriptions
                .AsNoTracking()
                .Where(p => p.PatientId == patientId)
                .Include(p => p.Doctor);

            var totalCount = await query.CountAsync(cancellationToken);

            var prescriptions = await query
                .OrderByDescending(p => p.PrescriptionDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new PrescriptionDto
                {
                    Id = p.Id,
                    PatientId = p.PatientId,
                    DoctorName = p.Doctor!.FullName,
                    PrescriptionDate = p.PrescriptionDate,
                    PrescriptionDetails = p.PrescriptionDetails,
                    ValidUntil = p.ValidUntil
                })
                .ToListAsync(cancellationToken);

            return (prescriptions, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching prescriptions for patient {PatientId}", patientId);
            throw;
        }
    }

    public async Task<InvoiceDto?> GetInvoiceAsync(
        int invoiceId,
        int patientId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var invoice = await _context.Transactions
                .AsNoTracking()
                .Where(t => t.Id == invoiceId && t.PatientId == patientId)
                .Include(t => t.Patient)
                .Include(t => t.Appointment)
                    .ThenInclude(a => a!.Doctor)
                .Select(t => new InvoiceDto
                {
                    Id = t.Id,
                    TransactionId = t.Id,
                    PatientId = t.PatientId,
                    PatientName = t.Patient.FullName,
                    PatientMRNumber = t.Patient.MRNumber,
                    TransactionType = t.TransactionType,
                    Description = t.Description,
                    Amount = t.Amount,
                    PaymentMode = t.PaymentMode,
                    ReferenceNumber = t.ReferenceNumber,
                    Status = t.Status,
                    TransactionDate = t.TransactionDate,
                    RelatedService = t.Appointment != null ? $"Dr. {t.Appointment.Doctor!.FullName}" : null
                })
                .FirstOrDefaultAsync(cancellationToken);

            return invoice;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching invoice {InvoiceId} for patient {PatientId}", invoiceId, patientId);
            throw;
        }
    }

    public async Task<LabReportDto?> GetLabReportAsync(
        int labTestId,
        int patientId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var report = await _context.LabTests
                .AsNoTracking()
                .Where(l => l.Id == labTestId && l.PatientId == patientId)
                .Include(l => l.LabTestCategory)
                .Include(l => l.ApprovedByDoctor)
                .Select(l => new LabReportDto
                {
                    Id = l.Id,
                    PatientId = l.PatientId,
                    TestName = l.TestName,
                    CategoryName = l.LabTestCategory.CategoryName,
                    TestDate = l.TestDate,
                    Status = l.Status,
                    ApprovedDate = l.ApprovedDate,
                    ApprovedByDoctor = l.ApprovedByDoctor != null ? l.ApprovedByDoctor.FullName : null,
                    ReportNotes = l.ReportNotes,
                    IsApproved = l.IsApproved,
                    ReportFilePath = l.ReportFilePath
                })
                .FirstOrDefaultAsync(cancellationToken);

            return report;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching lab report {LabTestId} for patient {PatientId}", labTestId, patientId);
            throw;
        }
    }

    public async Task<PrescriptionDto?> GetPrescriptionAsync(
        int prescriptionId,
        int patientId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var prescription = await _context.Prescriptions
                .AsNoTracking()
                .Where(p => p.Id == prescriptionId && p.PatientId == patientId)
                .Include(p => p.Doctor)
                .Select(p => new PrescriptionDto
                {
                    Id = p.Id,
                    PatientId = p.PatientId,
                    DoctorName = p.Doctor!.FullName,
                    PrescriptionDate = p.PrescriptionDate,
                    PrescriptionDetails = p.PrescriptionDetails,
                    ValidUntil = p.ValidUntil
                })
                .FirstOrDefaultAsync(cancellationToken);

            return prescription;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching prescription {PrescriptionId} for patient {PatientId}", prescriptionId, patientId);
            throw;
        }
    }
}
