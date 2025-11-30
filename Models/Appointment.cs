using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MMGC.Models;

public class Appointment
{
    [Key]
    public int Id { get; set; }

    [Required]
    [Display(Name = "Patient")]
    public int PatientId { get; set; }

    [Display(Name = "Doctor")]
    public int? DoctorId { get; set; }

    [Display(Name = "Nurse")]
    public int? NurseId { get; set; }

    [Required]
    [Display(Name = "Appointment Date")]
    [DataType(DataType.DateTime)]
    public DateTime AppointmentDate { get; set; }

    [StringLength(50)]
    [Display(Name = "Appointment Type")]
    public string AppointmentType { get; set; } = "General"; // General, Follow-up, Emergency, etc.

    [StringLength(20)]
    [Display(Name = "Status")]
    public string Status { get; set; } = "Scheduled"; // Scheduled, Confirmed, Completed, Cancelled, No-Show

    [StringLength(500)]
    [Display(Name = "Reason")]
    public string? Reason { get; set; }

    [StringLength(1000)]
    [Display(Name = "Notes")]
    public string? Notes { get; set; }

    [Display(Name = "Consultation Fee")]
    [Column(TypeName = "decimal(18,2)")]
    public decimal ConsultationFee { get; set; }

    [Display(Name = "SMS Sent")]
    public bool SMSSent { get; set; }

    [Display(Name = "WhatsApp Sent")]
    public bool WhatsAppSent { get; set; }

    [Display(Name = "Created Date")]
    public DateTime CreatedDate { get; set; } = DateTime.Now;

    [Display(Name = "Updated Date")]
    public DateTime? UpdatedDate { get; set; }

    [StringLength(450)]
    [Display(Name = "Created By")]
    public string? CreatedBy { get; set; }

    // Navigation properties
    [ForeignKey("PatientId")]
    public virtual Patient Patient { get; set; } = null!;

    [ForeignKey("DoctorId")]
    public virtual Doctor? Doctor { get; set; }

    [ForeignKey("NurseId")]
    public virtual Nurse? Nurse { get; set; }
}
