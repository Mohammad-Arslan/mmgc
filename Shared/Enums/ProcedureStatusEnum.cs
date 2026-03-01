namespace MMGC.Shared.Enums;

/// <summary>
/// Represents the status of a procedure request throughout its approval workflow.
/// </summary>
public enum ProcedureStatusEnum
{
    /// <summary>
    /// Procedure has been requested by patient or doctor.
    /// </summary>
    Requested = 0,

    /// <summary>
    /// Procedure request is approved by doctor.
    /// </summary>
    Approved = 1,

    /// <summary>
    /// Procedure is scheduled.
    /// </summary>
    Scheduled = 2,

    /// <summary>
    /// Procedure has been completed.
    /// </summary>
    Completed = 3,

    /// <summary>
    /// Procedure request has been rejected.
    /// </summary>
    Rejected = 4,

    /// <summary>
    /// Procedure has been cancelled.
    /// </summary>
    Cancelled = 5
}
