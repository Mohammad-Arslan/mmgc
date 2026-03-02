using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MMGC.Shared.Enums;

namespace MMGC.Models;

public class Appointment
{
    [Key]
    public int Id { get; set; }

    // Make nullable to allow empty option from the select to bind to null.
    // Use Range validation to ensure a valid patient is selected (null or 0 will fail).
    [Range(1, int.MaxValue, ErrorMessage = "Please select a patient.")]
    [Display(Name = "Patient")]
    public int? PatientId { get; set; }

    [Display(Name = "Doctor")]
    public int? DoctorId { get; set; }

    [Display(Name = "Nurse")]
    public int? NurseId { get; set; }

    [Required]
    [Display(Name = "Appointment Date")]
    [DataType(DataType.DateTime)]
    public DateTime AppointmentDate { get; set; }

    /// <summary>
    /// End time of the appointment (for slot-based scheduling).
    /// </summary>
    [Display(Name = "Appointment End Time")]
    [DataType(DataType.DateTime)]
    public DateTime? AppointmentEndTime { get; set; }

    [StringLength(50)]
    [Display(Name = "Appointment Type")]
    public string AppointmentType { get; set; } = "General"; // General, Follow-up, Emergency, etc.

    /// <summary>
    /// Status as enum for type-safe operations (replaces string Status).
    /// </summary>
    [Required]
    public AppointmentStatusEnum StatusEnum { get; set; } = AppointmentStatusEnum.Scheduled;

    // Legacy field, kept for backward compatibility
    [StringLength(20)]
    [Display(Name = "Status")]
    public string Status { get; set; } = "Scheduled";

    /// <summary>
    /// Consultation fee for the appointment (from Doctor).
    /// </summary>
    [Display(Name = "Consultation Fee")]
    [Column(TypeName = "decimal(18,2)")]
    public decimal ConsultationFee { get; set; }

    /// <summary>
    /// Date and time when the appointment was created.
    /// </summary>
    [Display(Name = "Created Date")]
    public DateTime CreatedDate { get; set; } = DateTime.Now;

    /// <summary>
    /// Date and time when the appointment was last updated.
    /// </summary>
    [Display(Name = "Updated Date")]
    public DateTime? UpdatedDate { get; set; }

    /// <summary>
    /// User ID who created this appointment.
    /// </summary>
    [StringLength(450)]
    [Display(Name = "Created By")]
    public string? CreatedBy { get; set; }

    /// <summary>
    /// SMS confirmation sent flag.
    /// </summary>
    [Display(Name = "SMS Sent")]
    public bool SMSSent { get; set; } = false;

    /// <summary>
    /// WhatsApp confirmation sent flag.
    /// </summary>
    [Display(Name = "WhatsApp Sent")]
    public bool WhatsAppSent { get; set; } = false;

    /// <summary>
    /// Concurrency token for preventing double-booking and concurrent modifications.
    /// </summary>
    [Timestamp]
    public byte[]? RowVersion { get; set; }

    // Navigation properties
    [ForeignKey("PatientId")]
    public virtual Patient? Patient { get; set; }

    [ForeignKey("DoctorId")]
    public virtual Doctor? Doctor { get; set; }

    [ForeignKey("NurseId")]
    public virtual Nurse? Nurse { get; set; }

    [StringLength(1000)]
    [Display(Name = "Reason")]
    public string? Reason { get; set; }

    [StringLength(500)]
    [Display(Name = "Notes")]
    public string? Notes { get; set; }
}
