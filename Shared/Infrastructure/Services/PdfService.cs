using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MMGC.Data;
using MMGC.Models;
using MMGC.Shared.Interfaces;

namespace MMGC.Shared.Infrastructure.Services;

/// <summary>
/// Production-grade PDF service for generating hospital-branded documents.
/// Uses simple HTML-to-PDF conversion with iTextSharp or similar.
/// For production use, integrate with a robust PDF library.
/// </summary>
public class PdfService : IPdfService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PdfService> _logger;
    private readonly INotificationLogService _notificationLogService;
    private readonly string _hospitalName = "Medicare Hospital System";
    private readonly string _hospitalAddress = "123 Medical Street, City, State 12345";
    private readonly string _hospitalPhone = "+1 (555) 123-4567";
    private readonly string _hospitalEmail = "info@medicarehospital.com";
    private readonly string _hospitalLogo = "Medicare";

    public PdfService(
        ApplicationDbContext context,
        ILogger<PdfService> logger,
        INotificationLogService notificationLogService)
    {
        _context = context;
        _logger = logger;
        _notificationLogService = notificationLogService;
    }

    public async Task<byte[]> GeneratePrescriptionPdfAsync(
        int prescriptionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var prescription = await _context.Prescriptions
                .AsNoTracking()
                .Include(p => p.Patient)
                .Include(p => p.Doctor)
                .FirstOrDefaultAsync(p => p.Id == prescriptionId, cancellationToken);

            if (prescription == null)
                throw new InvalidOperationException($"Prescription {prescriptionId} not found.");

            var html = GeneratePrescriptionHtml(prescription);
            var pdfBytes = HtmlToPdf(html, "Prescription");

            // Log document generation
            await LogDocumentGenerationAsync(
                "Prescription",
                prescriptionId,
                prescription.PatientId,
                pdfBytes.Length,
                cancellationToken);

            return pdfBytes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating prescription PDF for ID {PrescriptionId}", prescriptionId);
            throw;
        }
    }

    public async Task<byte[]> GenerateInvoicePdfAsync(
        int transactionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var transaction = await _context.Transactions
                .AsNoTracking()
                .Include(t => t.Patient)
                .Include(t => t.Appointment)
                    .ThenInclude(a => a!.Doctor)
                .FirstOrDefaultAsync(t => t.Id == transactionId, cancellationToken);

            if (transaction == null)
                throw new InvalidOperationException($"Transaction {transactionId} not found.");

            var html = GenerateInvoiceHtml(transaction);
            var pdfBytes = HtmlToPdf(html, "Invoice");

            // Log document generation
            await LogDocumentGenerationAsync(
                "Invoice",
                transactionId,
                transaction.PatientId,
                pdfBytes.Length,
                cancellationToken);

            return pdfBytes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating invoice PDF for transaction ID {TransactionId}", transactionId);
            throw;
        }
    }

    public async Task<byte[]> GenerateLabReportPdfAsync(
        int labTestId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var labTest = await _context.LabTests
                .AsNoTracking()
                .Include(l => l.Patient)
                .Include(l => l.LabTestCategory)
                .Include(l => l.ApprovedByDoctor)
                .FirstOrDefaultAsync(l => l.Id == labTestId, cancellationToken);

            if (labTest == null)
                throw new InvalidOperationException($"Lab test {labTestId} not found.");

            var html = GenerateLabReportHtml(labTest);
            var pdfBytes = HtmlToPdf(html, "LabReport");

            // Log document generation
            await LogDocumentGenerationAsync(
                "LabReport",
                labTestId,
                labTest.PatientId,
                pdfBytes.Length,
                cancellationToken);

            return pdfBytes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating lab report PDF for ID {LabTestId}", labTestId);
            throw;
        }
    }

    public async Task<byte[]> GenerateProcedureReportPdfAsync(
        int procedureId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var procedure = await _context.Procedures
                .AsNoTracking()
                .Include(p => p.Patient)
                .Include(p => p.Doctor)
                .FirstOrDefaultAsync(p => p.Id == procedureId, cancellationToken);

            if (procedure == null)
                throw new InvalidOperationException($"Procedure {procedureId} not found.");

            var html = GenerateProcedureReportHtml(procedure);
            var pdfBytes = HtmlToPdf(html, "ProcedureReport");

            // Log document generation
            await LogDocumentGenerationAsync(
                "ProcedureReport",
                procedureId,
                procedure.PatientId ?? 0,
                pdfBytes.Length,
                cancellationToken);

            return pdfBytes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating procedure report PDF for ID {ProcedureId}", procedureId);
            throw;
        }
    }

    public async Task<byte[]> GenerateAppointmentPdfAsync(
        int appointmentId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var appointment = await _context.Appointments
                .AsNoTracking()
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .FirstOrDefaultAsync(a => a.Id == appointmentId, cancellationToken);

            if (appointment == null)
                throw new InvalidOperationException($"Appointment {appointmentId} not found.");

            var html = GenerateAppointmentConfirmationHtml(appointment);
            var pdfBytes = HtmlToPdf(html, "AppointmentConfirmation");

            // Log document generation
            await LogDocumentGenerationAsync(
                "AppointmentConfirmation",
                appointmentId,
                appointment.PatientId ?? 0,
                pdfBytes.Length,
                cancellationToken);

            return pdfBytes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating appointment PDF for ID {AppointmentId}", appointmentId);
            throw;
        }
    }

    private string GeneratePrescriptionHtml(Prescription prescription)
    {
        var html = new StringBuilder();
        html.AppendLine(BuildHtmlHeader("Prescription"));
        html.AppendLine($"""
            <div class="content">
                <h2>Prescription</h2>
                <div class="patient-info">
                    <p><strong>Patient Name:</strong> {prescription.Patient?.FullName}</p>
                    <p><strong>MR Number:</strong> {prescription.Patient?.MRNumber}</p>
                    <p><strong>Date of Birth:</strong> {prescription.Patient?.DateOfBirth:yyyy-MM-dd}</p>
                </div>
                
                <div class="doctor-info">
                    <p><strong>Prescribed By:</strong> Dr. {prescription.Doctor?.FullName}</p>
                    <p><strong>Specialization:</strong> {prescription.Doctor?.Specialization}</p>
                    <p><strong>Prescription Date:</strong> {prescription.PrescriptionDate:yyyy-MM-dd HH:mm}</p>
                </div>
                
                <div class="prescription-details">
                    <h3>Medications</h3>
                    <p>{HtmlEncode(prescription.PrescriptionDetails)}</p>
                </div>
                
                <div class="validity">
                    <p><strong>Valid Until:</strong> {(prescription.ValidUntil?.ToString("yyyy-MM-dd") ?? "Until refilled")}</p>
                </div>
                
                <div class="notes">
                    <p><em>Instructions: Follow doctor's instructions carefully. Do not share with others.</em></p>
                </div>
            </div>
            """);
        html.AppendLine(BuildHtmlFooter());
        return html.ToString();
    }

    private string GenerateInvoiceHtml(Transaction transaction)
    {
        var html = new StringBuilder();
        html.AppendLine(BuildHtmlHeader("Invoice"));
        html.AppendLine($"""
            <div class="content">
                <h2>Invoice</h2>
                <div class="invoice-header">
                    <p><strong>Invoice Number:</strong> INV-{transaction.Id:D6}</p>
                    <p><strong>Date:</strong> {transaction.TransactionDate:yyyy-MM-dd HH:mm}</p>
                </div>
                
                <div class="patient-info">
                    <h3>Bill To</h3>
                    <p><strong>{transaction.Patient?.FullName}</strong></p>
                    <p>MR Number: {transaction.Patient?.MRNumber}</p>
                    <p>Contact: {transaction.Patient?.ContactNumber}</p>
                </div>
                
                <table class="invoice-table">
                    <thead>
                        <tr>
                            <th>Description</th>
                            <th>Amount</th>
                            <th>Status</th>
                        </tr>
                    </thead>
                    <tbody>
                        <tr>
                            <td>{HtmlEncode(transaction.Description)}</td>
                            <td>${transaction.Amount:F2}</td>
                            <td>{transaction.Status}</td>
                        </tr>
                    </tbody>
                    <tfoot>
                        <tr>
                            <th>Total:</th>
                            <th>${transaction.Amount:F2}</th>
                            <th></th>
                        </tr>
                    </tfoot>
                </table>
                
                <div class="payment-info">
                    <p><strong>Payment Mode:</strong> {transaction.PaymentMode}</p>
                    {(string.IsNullOrEmpty(transaction.ReferenceNumber) ? "" : $"<p><strong>Reference Number:</strong> {transaction.ReferenceNumber}</p>")}
                </div>
            </div>
            """);
        html.AppendLine(BuildHtmlFooter());
        return html.ToString();
    }

    private string GenerateLabReportHtml(LabTest labTest)
    {
        var html = new StringBuilder();
        html.AppendLine(BuildHtmlHeader("Lab Report"));
        html.AppendLine($"""
            <div class="content">
                <h2>Laboratory Test Report</h2>
                <div class="patient-info">
                    <p><strong>Patient Name:</strong> {labTest.Patient?.FullName}</p>
                    <p><strong>MR Number:</strong> {labTest.Patient?.MRNumber}</p>
                    <p><strong>Age/DOB:</strong> {labTest.Patient?.DateOfBirth:yyyy-MM-dd}</p>
                    <p><strong>Gender:</strong> {labTest.Patient?.Gender}</p>
                </div>
                
                <div class="test-info">
                    <p><strong>Test Name:</strong> {labTest.TestName}</p>
                    <p><strong>Category:</strong> {labTest.LabTestCategory?.CategoryName}</p>
                    <p><strong>Test Date:</strong> {labTest.TestDate:yyyy-MM-dd HH:mm}</p>
                    <p><strong>Status:</strong> {labTest.Status}</p>
                </div>
                
                <div class="report-details">
                    <h3>Results</h3>
                    <p>{HtmlEncode(labTest.ReportNotes ?? "Results pending")}</p>
                </div>
                
                {(labTest.ApprovedByDoctor != null ? $"""
                <div class="approval-info">
                    <p><strong>Approved By:</strong> Dr. {labTest.ApprovedByDoctor.FullName}</p>
                    <p><strong>Approved Date:</strong> {labTest.ApprovedDate:yyyy-MM-dd HH:mm}</p>
                </div>
                """ : "")}
            </div>
            """);
        html.AppendLine(BuildHtmlFooter());
        return html.ToString();
    }

    private string GenerateProcedureReportHtml(Procedure procedure)
    {
        var html = new StringBuilder();
        html.AppendLine(BuildHtmlHeader("Procedure Report"));
        html.AppendLine($"""
            <div class="content">
                <h2>Procedure Report</h2>
                <div class="patient-info">
                    <p><strong>Patient Name:</strong> {procedure.Patient?.FullName}</p>
                    <p><strong>MR Number:</strong> {procedure.Patient?.MRNumber}</p>
                </div>
                
                <div class="procedure-info">
                    <p><strong>Procedure Type:</strong> {procedure.ProcedureType}</p>
                    <p><strong>Procedure Date:</strong> {procedure.ProcedureDate:yyyy-MM-dd HH:mm}</p>
                    <p><strong>Status:</strong> {procedure.Status}</p>
                </div>
                
                <div class="doctor-info">
                    <p><strong>Performed By:</strong> Dr. {procedure.Doctor?.FullName}</p>
                </div>
                
                <div class="report-details">
                    <h3>Findings</h3>
                    <p>{HtmlEncode(procedure.Findings ?? "No findings recorded")}</p>
                </div>
            </div>
            """);
        html.AppendLine(BuildHtmlFooter());
        return html.ToString();
    }

    private string GenerateAppointmentConfirmationHtml(Appointment appointment)
    {
        var html = new StringBuilder();
        html.AppendLine(BuildHtmlHeader("Appointment Confirmation"));
        html.AppendLine($"""
            <div class="content">
                <h2>Appointment Confirmation</h2>
                <div class="confirmation-header">
                    <p style="color: green; font-weight: bold;">Your appointment is confirmed</p>
                </div>
                
                <div class="patient-info">
                    <p><strong>Patient Name:</strong> {appointment.Patient?.FullName}</p>
                    <p><strong>MR Number:</strong> {appointment.Patient?.MRNumber}</p>
                </div>
                
                <div class="appointment-details">
                    <p><strong>Doctor:</strong> Dr. {appointment.Doctor?.FullName}</p>
                    <p><strong>Specialization:</strong> {appointment.Doctor?.Specialization}</p>
                    <p><strong>Appointment Date & Time:</strong> {appointment.AppointmentDate:dddd, MMMM dd, yyyy HH:mm}</p>
                    <p><strong>Appointment Type:</strong> {appointment.AppointmentType}</p>
                    <p><strong>Status:</strong> {appointment.Status}</p>
                </div>
                
                <div class="important-notes">
                    <h3>Important Notes:</h3>
                    <ul>
                        <li>Please arrive 10 minutes before your scheduled time</li>
                        <li>Bring your ID and insurance card</li>
                        <li>If you need to reschedule, contact us at least 24 hours in advance</li>
                        <li>Our address: {_hospitalAddress}</li>
                        <li>Phone: {_hospitalPhone}</li>
                    </ul>
                </div>
            </div>
            """);
        html.AppendLine(BuildHtmlFooter());
        return html.ToString();
    }

    private string BuildHtmlHeader(string documentTitle)
    {
        var css = @"
            body { font-family: Arial, sans-serif; color: #333; }
            .header { border-bottom: 2px solid #0066cc; padding-bottom: 20px; margin-bottom: 30px; }
            .hospital-info { text-align: center; margin-bottom: 10px; }
            .hospital-name { font-size: 24px; font-weight: bold; color: #0066cc; }
            .hospital-details { font-size: 11px; color: #666; margin-top: 5px; }
            .content { margin: 20px 0; }
            h2 { color: #0066cc; border-bottom: 1px solid #ddd; padding-bottom: 10px; }
            h3 { color: #333; margin-top: 20px; }
            .patient-info, .doctor-info, .procedure-info, .appointment-details { 
                background: #f9f9f9; padding: 15px; margin: 15px 0; border-left: 3px solid #0066cc;
            }
            table { width: 100%; border-collapse: collapse; margin: 20px 0; }
            table th, table td { padding: 10px; text-align: left; border-bottom: 1px solid #ddd; }
            table th { background: #0066cc; color: white; }
            .footer { 
                margin-top: 40px; padding-top: 20px; border-top: 1px solid #ddd; 
                text-align: center; font-size: 10px; color: #999;
            }
            .confirmation-header { padding: 10px; text-align: center; margin: 20px 0; }
            ul { margin: 10px 0; padding-left: 20px; }
            .validity, .payment-info, .important-notes { 
                background: #f0f8ff; padding: 15px; margin: 15px 0; border: 1px solid #0066cc;
            }
        ";

        return $"""
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset="utf-8">
                <title>{documentTitle}</title>
                <style>
                    {css}
                </style>
            </head>
            <body>
                <div class="header">
                    <div class="hospital-info">
                        <div class="hospital-name">{_hospitalName}</div>
                        <div class="hospital-details">
                            {_hospitalAddress}<br>
                            Phone: {_hospitalPhone} | Email: {_hospitalEmail}
                        </div>
                    </div>
                    <div style="text-align: right; font-size: 11px; color: #666;">
                        Generated on {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC
                    </div>
                </div>
            """;
    }

    private string BuildHtmlFooter()
    {
        return $"""
                <div class="footer">
                    <p><strong>Confidentiality Notice:</strong> This document contains confidential and privileged information. 
                    If you are not the intended recipient, please destroy this document and notify the sender immediately.</p>
                    <p>{_hospitalName} - All Rights Reserved © {DateTime.Now.Year}</p>
                </div>
            </body>
            </html>
            """;
    }

    /// <summary>
    /// Placeholder for HTML to PDF conversion.
    /// In production, use a library like iTextSharp, SelectPdf, or sync to external service.
    /// </summary>
    private byte[] HtmlToPdf(string html, string documentName)
    {
        // TODO: Integrate actual PDF library (iTextSharp, SelectPdf, etc.)
        // For now, return HTML as UTF-8 bytes (for demonstration)
        // In production, this must actually convert to PDF
        
        _logger.LogWarning(
            "PDF generation for {DocumentName} is using placeholder. Integrate real PDF library in production.",
            documentName);

        return Encoding.UTF8.GetBytes(html);
    }

    private string HtmlEncode(string text)
    {
        return System.Net.WebUtility.HtmlEncode(text);
    }

    private async Task LogDocumentGenerationAsync(
        string documentType,
        int documentId,
        int patientId,
        int fileSizeBytes,
        CancellationToken cancellationToken)
    {
        try
        {
            var auditLog = new DocumentAuditLog
            {
                DocumentType = documentType,
                EntityId = documentId,
                PatientId = patientId,
                FileSizeBytes = fileSizeBytes,
                GeneratedAt = DateTime.UtcNow,
                RequestedBy = "System", // TODO: Get from context
                FileName = $"{documentType}-{documentId:D6}.pdf"
            };

            _context.DocumentAuditLogs.Add(auditLog);
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log document generation for {DocumentType}", documentType);
            // Don't throw - logging failure shouldn't break PDF generation
        }
    }
}
