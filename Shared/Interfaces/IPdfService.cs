using MMGC.Shared.DTOs;

namespace MMGC.Shared.Interfaces;

/// <summary>
/// Service contract for PDF document generation.
/// Handles generation of hospital-branded PDF documents for prescriptions, invoices, and reports.
/// </summary>
public interface IPdfService
{
    /// <summary>
    /// Generates a PDF for a prescription document.
    /// </summary>
    /// <param name="prescriptionId">The prescription ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>PDF file content as byte array.</returns>
    Task<byte[]> GeneratePrescriptionPdfAsync(int prescriptionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a PDF for an invoice/transaction document.
    /// </summary>
    /// <param name="transactionId">The transaction ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>PDF file content as byte array.</returns>
    Task<byte[]> GenerateInvoicePdfAsync(int transactionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a PDF for a lab test report.
    /// </summary>
    /// <param name="labTestId">The lab test ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>PDF file content as byte array.</returns>
    Task<byte[]> GenerateLabReportPdfAsync(int labTestId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a PDF for an ultrasound/procedure report.
    /// </summary>
    /// <param name="procedureId">The procedure ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>PDF file content as byte array.</returns>
    Task<byte[]> GenerateProcedureReportPdfAsync(int procedureId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a PDF for appointment summary/confirmation.
    /// </summary>
    /// <param name="appointmentId">The appointment ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>PDF file content as byte array.</returns>
    Task<byte[]> GenerateAppointmentPdfAsync(int appointmentId, CancellationToken cancellationToken = default);
}
