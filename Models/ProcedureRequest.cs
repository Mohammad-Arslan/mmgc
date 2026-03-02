using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MMGC.Shared.Enums;

namespace MMGC.Models;

/// <summary>
/// Represents a patient's formal request for a medical procedure (Delivery, C-Section, etc.)
/// that requires doctor approval before scheduling.
/// </summary>
public class ProcedureRequest
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Patient requesting the procedure.
    /// </summary>
    [Required]
    public int PatientId { get; set; }

    /// <summary>
    /// Doctor who will review and approve/reject the request.
    /// </summary>
    public int? DoctorId { get; set; }

    /// <summary>
    /// Type of procedure being requested.
    /// </summary>
    [Required]
    [StringLength(100)]
    [Display(Name = "Procedure Type")]
    public string ProcedureType { get; set; } = string.Empty;

    /// <summary>
    /// Clinical indication or reason for the procedure.
    /// </summary>
    [Required]
    [StringLength(1000)]
    [Display(Name = "Reason for Procedure")]
    public string ReasonForProcedure { get; set; } = string.Empty;

    /// <summary>
    /// Preferred date for the procedure (optional, for informational purposes).
    /// </summary>
    [Display(Name = "Requested Date")]
    [DataType(DataType.DateTime)]
    public DateTime? RequestedDate { get; set; }

    /// <summary>
    /// Current status in the approval workflow.
    /// </summary>
    [Required]
    public ProcedureStatusEnum Status { get; set; } = ProcedureStatusEnum.Requested;

    /// <summary>
    /// Doctor's comments on approval or rejection.
    /// </summary>
    [StringLength(1000)]
    [Display(Name = "Approval Comments")]
    public string? ApprovalComments { get; set; }

    /// <summary>
    /// Date and time when the request was created.
    /// </summary>
    [Required]
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Date and time when the request was last reviewed by a doctor.
    /// </summary>
    public DateTime? ReviewedDate { get; set; }

    /// <summary>
    /// Date and time when the procedure was scheduled.
    /// </summary>
    public DateTime? ScheduledDate { get; set; }

    /// <summary>
    /// If approved and scheduled, this references the created Procedure record.
    /// </summary>
    public int? LinkedProcedureId { get; set; }

    /// <summary>
    /// User ID of who created this request.
    /// </summary>
    [StringLength(450)]
    public string? CreatedBy { get; set; }

    // Navigation properties
    [ForeignKey(nameof(PatientId))]
    public virtual Patient? Patient { get; set; }

    [ForeignKey(nameof(DoctorId))]
    public virtual Doctor? AssignedDoctor { get; set; }

    [ForeignKey(nameof(LinkedProcedureId))]
    public virtual Procedure? LinkedProcedure { get; set; }
}
