using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MMGC.Models;

public class Procedure
{
    [Key]
    public int Id { get; set; }

    [Required]
    [Display(Name = "Patient")]
    public int PatientId { get; set; }

    [Required]
    [Display(Name = "Doctor")]
    public int DoctorId { get; set; }

    [Display(Name = "Nurse")]
    public int? NurseId { get; set; }

    [Required]
    [StringLength(100)]
    [Display(Name = "Procedure Name")]
    public string ProcedureName { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    [Display(Name = "Procedure Type")]
    public string ProcedureType { get; set; } = string.Empty; // Normal Delivery, C-section, Ultrasound, Gynaecological, Surgery, OPD, IPD, Other

    [Display(Name = "Procedure Date")]
    [DataType(DataType.DateTime)]
    public DateTime ProcedureDate { get; set; } = DateTime.Now;

    [StringLength(2000)]
    [Display(Name = "Treatment Notes")]
    public string? TreatmentNotes { get; set; }

    [StringLength(2000)]
    [Display(Name = "Prescription")]
    public string? Prescription { get; set; }

    [Display(Name = "Procedure Fee")]
    [Column(TypeName = "decimal(18,2)")]
    public decimal ProcedureFee { get; set; }

    [StringLength(20)]
    [Display(Name = "Status")]
    public string Status { get; set; } = "Scheduled"; // Scheduled, In Progress, Completed, Cancelled

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
    public virtual Doctor Doctor { get; set; } = null!;

    [ForeignKey("NurseId")]
    public virtual Nurse? Nurse { get; set; }

    public virtual ICollection<LabTest> LabTests { get; set; } = new List<LabTest>();
}
