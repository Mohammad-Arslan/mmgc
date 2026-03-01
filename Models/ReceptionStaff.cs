using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MMGC.Models;

public class ReceptionStaff
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    [Display(Name = "First Name")]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    [Display(Name = "Last Name")]
    public string LastName { get; set; } = string.Empty;

    [NotMapped]
    [Display(Name = "Full Name")]
    public string FullName => $"{FirstName} {LastName}";

    [StringLength(15)]
    [Display(Name = "Contact Number")]
    public string? ContactNumber { get; set; }

    [EmailAddress]
    [StringLength(100)]
    [Display(Name = "Email")]
    public string? Email { get; set; }

    [StringLength(100)]
    [Display(Name = "Department")]
    public string? Department { get; set; }

    [StringLength(50)]
    [Display(Name = "Employee ID")]
    public string? EmployeeId { get; set; }

    [Display(Name = "Is Active")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "Created Date")]
    public DateTime CreatedDate { get; set; } = DateTime.Now;

    [Display(Name = "Updated Date")]
    public DateTime? UpdatedDate { get; set; }

    [StringLength(450)]
    [Display(Name = "User ID")]
    public string? UserId { get; set; } // Link to ApplicationUser
}
