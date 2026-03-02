using MMGC.Shared.DTOs;

namespace MMGC.Shared.Interfaces;

/// <summary>
/// Service contract for managing procedure request approval workflow.
/// Handles status transitions: Requested → Approved → Scheduled → Completed
/// </summary>
public interface IProcedureWorkflowService
{
    /// <summary>
    /// Creates a new procedure request from a patient.
    /// </summary>
    /// <param name="patientId">The requesting patient's ID.</param>
    /// <param name="doctorId">The doctor who will review the request.</param>
    /// <param name="procedureType">Type of procedure requested.</param>
    /// <param name="reason">Clinical reason for procedure.</param>
    /// <param name="requestedDate">Preferred date for procedure.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Created procedure request DTO.</returns>
    Task<ProcedureRequestDto> CreateProcedureRequestAsync(
        int patientId,
        int? doctorId,
        string procedureType,
        string reason,
        DateTime? requestedDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a specific procedure request.
    /// </summary>
    Task<ProcedureRequestDto?> GetProcedureRequestAsync(int procedureRequestId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all pending procedure requests for a specific doctor.
    /// </summary>
    /// <param name="doctorId">The doctor's ID.</param>
    /// <param name="pageNumber">Page number for pagination.</param>
    /// <param name="pageSize">Page size for pagination.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paginated list of pending requests.</returns>
    Task<(List<ProcedureRequestDto> Items, int TotalCount)> GetPendingRequestsForDoctorAsync(
        int doctorId,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all procedure requests for a patient.
    /// </summary>
    Task<(List<ProcedureRequestDto> Items, int TotalCount)> GetPatientProcedureRequestsAsync(
        int patientId,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Approves a procedure request and optionally schedules it.
    /// Sends notification to patient.
    /// </summary>
    /// <param name="procedureRequestId">The request to approve.</param>
    /// <param name="doctorId">The approving doctor's ID (authorization).</param>
    /// <param name="approvalComments">Optional approval comments.</param>
    /// <param name="scheduledDate">Optional date to schedule the procedure immediately.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Updated procedure request DTO.</returns>
    /// <exception cref="UnauthorizedException">Thrown if doctor is not authorized.</exception>
    /// <exception cref="InvalidProcedureStateTransitionException">Thrown if state transition is invalid.</exception>
    Task<ProcedureRequestDto> ApproveProcedureRequestAsync(
        int procedureRequestId,
        int doctorId,
        string? approvalComments = null,
        DateTime? scheduledDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Rejects a procedure request.
    /// Sends notification to patient.
    /// </summary>
    /// <param name="procedureRequestId">The request to reject.</param>
    /// <param name="doctorId">The rejecting doctor's ID (authorization).</param>
    /// <param name="rejectionReason">Reason for rejection.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Updated procedure request DTO.</returns>
    Task<ProcedureRequestDto> RejectProcedureRequestAsync(
        int procedureRequestId,
        int doctorId,
        string rejectionReason,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Schedules an approved procedure for a specific date/time.
    /// </summary>
    /// <param name="procedureRequestId">The approved request to schedule.</param>
    /// <param name="scheduledDateTime">The date and time to schedule.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Procedure ID of the scheduled procedure.</returns>
    Task<int> ScheduleApprovedProcedureAsync(
        int procedureRequestId,
        DateTime scheduledDateTime,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels a procedure request or scheduled procedure.
    /// </summary>
    /// <param name="procedureRequestId">The request to cancel.</param>
    /// <param name="cancellationReason">Reason for cancellation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Updated procedure request DTO.</returns>
    Task<ProcedureRequestDto> CancelProcedureRequestAsync(
        int procedureRequestId,
        string cancellationReason,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks an approved and scheduled procedure as completed.
    /// </summary>
    /// <param name="procedureRequestId">The completed request.</param>
    /// <param name="completionNotes">Optional notes about the procedure.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Updated procedure request DTO.</returns>
    Task<ProcedureRequestDto> CompleteProcedureAsync(
        int procedureRequestId,
        string? completionNotes = null,
        CancellationToken cancellationToken = default);
}
