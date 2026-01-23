using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MMGC.Models;

public class PatientVital
{
    [Key]
    public int Id { get; set; }

    [Required]
    [Display(Name = "Patient")]
    public int PatientId { get; set; }

    [Display(Name = "Procedure")]
    public int? ProcedureId { get; set; }

    [Display(Name = "Appointment")]
    public int? AppointmentId { get; set; }

    [Display(Name = "Nurse")]
    public int? NurseId { get; set; }

    [Required]
    [Display(Name = "Recorded Date")]
    [DataType(DataType.DateTime)]
    public DateTime RecordedDate { get; set; } = DateTime.Now;

    [Display(Name = "Blood Pressure (Systolic)")]
    public int? BloodPressureSystolic { get; set; }

    [Display(Name = "Blood Pressure (Diastolic)")]
    public int? BloodPressureDiastolic { get; set; }

    [Display(Name = "Temperature (°F)")]
    [Column(TypeName = "decimal(5,2)")]
    public decimal? Temperature { get; set; }

    [Display(Name = "Pulse (BPM)")]
    public int? Pulse { get; set; }

    [Display(Name = "Respiratory Rate")]
    public int? RespiratoryRate { get; set; }

    [Display(Name = "Oxygen Saturation (%)")]
    public int? OxygenSaturation { get; set; }

    [Display(Name = "Weight (kg)")]
    [Column(TypeName = "decimal(5,2)")]
    public decimal? Weight { get; set; }

    [Display(Name = "Height (cm)")]
    [Column(TypeName = "decimal(5,2)")]
    public decimal? Height { get; set; }

    [StringLength(500)]
    [Display(Name = "Additional Notes")]
    public string? Notes { get; set; }

    [Display(Name = "Created Date")]
    public DateTime CreatedDate { get; set; } = DateTime.Now;

    [StringLength(450)]
    [Display(Name = "Recorded By")]
    public string? RecordedBy { get; set; }

    // Navigation properties
    [ForeignKey("PatientId")]
    public virtual Patient Patient { get; set; } = null!;

    [ForeignKey("ProcedureId")]
    public virtual Procedure? Procedure { get; set; }

    [ForeignKey("AppointmentId")]
    public virtual Appointment? Appointment { get; set; }

    [ForeignKey("NurseId")]
    public virtual Nurse? Nurse { get; set; }
}

