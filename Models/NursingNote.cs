using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MMGC.Models;

public class NursingNote
{
    [Key]
    public int Id { get; set; }

    // Make nullable to allow empty option from the select to bind to null.
    // Keep Required so validation still forces user to pick one.
    [Required(ErrorMessage = "Please select a patient.")]
    [Range(1, int.MaxValue, ErrorMessage = "Please select a valid patient.")]
    [Display(Name = "Patient")]
    public int? PatientId { get; set; }

    [Display(Name = "Procedure")]
    public int? ProcedureId { get; set; }

    [Display(Name = "Appointment")]
    public int? AppointmentId { get; set; }

    // Make nullable to allow empty option from the select to bind to null.
    // Keep Required so validation still forces user to pick one.
    [Required(ErrorMessage = "Please select a nurse.")]
    [Range(1, int.MaxValue, ErrorMessage = "Please select a valid nurse.")]
    [Display(Name = "Nurse")]
    public int? NurseId { get; set; }

    [Required]
    [Display(Name = "Note Date")]
    [DataType(DataType.DateTime)]
    public DateTime NoteDate { get; set; } = DateTime.Now;

    [StringLength(2000)]
    [Display(Name = "Nursing Notes")]
    public string? Notes { get; set; }

    [StringLength(500)]
    [Display(Name = "Vitals")]
    public string? Vitals { get; set; } // JSON or formatted string for BP, Temperature, Pulse, etc.

    [StringLength(1000)]
    [Display(Name = "Patient Progress")]
    public string? PatientProgress { get; set; }

    [StringLength(500)]
    [Display(Name = "Medications Administered")]
    public string? MedicationsAdministered { get; set; }

    [Display(Name = "Created Date")]
    public DateTime CreatedDate { get; set; } = DateTime.Now;

    [StringLength(450)]
    [Display(Name = "Created By")]
    public string? CreatedBy { get; set; }

    // Navigation properties
    [ForeignKey("PatientId")]
    public virtual Patient? Patient { get; set; }

    [ForeignKey("ProcedureId")]
    public virtual Procedure? Procedure { get; set; }

    [ForeignKey("AppointmentId")]
    public virtual Appointment? Appointment { get; set; }

    [ForeignKey("NurseId")]
    public virtual Nurse? Nurse { get; set; }
}

