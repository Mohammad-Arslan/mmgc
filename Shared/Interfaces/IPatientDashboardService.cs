using MMGC.Shared.DTOs;

namespace MMGC.Shared.Interfaces;

/// <summary>
/// Service contract for patient dashboard aggregation.
/// Aggregates data from multiple services to build a comprehensive patient portal.
/// </summary>
public interface IPatientDashboardService
{
    /// <summary>
    /// Retrieves complete dashboard data for a specific patient.
    /// </summary>
    /// <param name="patientId">The patient's ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Complete patient dashboard DTO.</returns>
    Task<PatientDashboardDto> GetPatientDashboardAsync(int patientId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves paginated appointment history for a patient.
    /// </summary>
    /// <param name="patientId">The patient's ID.</param>
    /// <param name="pageNumber">Page number (1-based).</param>
    /// <param name="pageSize">Number of records per page.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paginated list of appointment items.</returns>
    Task<(List<DashboardItemDto> Items, int TotalCount)> GetAppointmentHistoryAsync(
        int patientId, 
        int pageNumber = 1, 
        int pageSize = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves paginated prescription history for a patient.
    /// </summary>
    Task<(List<DashboardItemDto> Items, int TotalCount)> GetPrescriptionHistoryAsync(
        int patientId,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves paginated lab test history for a patient.
    /// </summary>
    Task<(List<DashboardItemDto> Items, int TotalCount)> GetLabTestHistoryAsync(
        int patientId,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves outstanding invoices for a patient.
    /// </summary>
    Task<(List<DashboardItemDto> Items, int TotalCount)> GetOutstandingInvoicesAsync(
        int patientId,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves procedure history for a patient.
    /// </summary>
    Task<(List<DashboardItemDto> Items, int TotalCount)> GetProcedureHistoryAsync(
        int patientId,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default);
}
