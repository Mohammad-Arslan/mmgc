namespace MMGC.Shared.DTOs;

/// <summary>
/// Data transfer object for procedure request/workflow.
/// </summary>
public class ProcedureRequestDto
{
    /// <summary>
    /// Unique identifier for the procedure request.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Patient ID requesting the procedure.
    /// </summary>
    public int PatientId { get; set; }

    /// <summary>
    /// Patient's full name.
    /// </summary>
    public string PatientName { get; set; } = string.Empty;

    /// <summary>
    /// Doctor ID who initiated or will approve the request.
    /// </summary>
    public int? DoctorId { get; set; }

    /// <summary>
    /// Doctor's name (if assigned).
    /// </summary>
    public string? DoctorName { get; set; }

    /// <summary>
    /// Type of procedure (Delivery, C-Section, Ultrasound, etc.).
    /// </summary>
    public string ProcedureType { get; set; } = string.Empty;

    /// <summary>
    /// Reason or clinical indication for the procedure.
    /// </summary>
    public string ReasonForProcedure { get; set; } = string.Empty;

    /// <summary>
    /// Requested date for the procedure.
    /// </summary>
    public DateTime? RequestedDate { get; set; }

    /// <summary>
    /// Current status of the request (Requested, Approved, Rejected, etc.).
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Doctor's comments on approval/rejection.
    /// </summary>
    public string? ApprovalComments { get; set; }

    /// <summary>
    /// Date when the request was created.
    /// </summary>
    public DateTime CreatedDate { get; set; }

    /// <summary>
    /// Date when the request was last reviewed.
    /// </summary>
    public DateTime? ReviewedDate { get; set; }

    /// <summary>
    /// Linked procedure ID (if approved and scheduled).
    /// </summary>
    public int? LinkedProcedureId { get; set; }

    /// <summary>
    /// Any attachments or supporting documents (URLs or file paths).
    /// </summary>
    public List<string>? Attachments { get; set; }
}
