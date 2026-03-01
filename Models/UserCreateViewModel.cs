using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MMGC.Models;

public class UserCreateViewModel
{
    [Required(ErrorMessage = "First Name is required")]
    [Display(Name = "First Name")]
    [StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last Name is required")]
    [Display(Name = "Last Name")]
    [StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email address")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "Phone Number")]
    [Phone(ErrorMessage = "Invalid phone number")]
    public string? PhoneNumber { get; set; }

    [Required(ErrorMessage = "Password is required")]
    [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirm Password is required")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm Password")]
    [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Role is required")]
    [Display(Name = "Role")]
    public string Role { get; set; } = string.Empty;

    [Display(Name = "Email Confirmed")]
    public bool EmailConfirmed { get; set; } = true;

    // Doctor-specific fields
    [Display(Name = "Specialization")]
    [StringLength(100)]
    public string? Specialization { get; set; }

    [Display(Name = "License Number")]
    [StringLength(50)]
    public string? LicenseNumber { get; set; }

    [Display(Name = "Consultation Fee")]
    [Column(TypeName = "decimal(18,2)")]
    public decimal? ConsultationFee { get; set; }

    [Display(Name = "Address")]
    [StringLength(500)]
    public string? Address { get; set; }

    // Nurse-specific fields
    [Display(Name = "Department")]
    [StringLength(100)]
    public string? Department { get; set; }

    // Staff-specific fields (ReceptionStaff, AccountsStaff, LabStaff)
    [Display(Name = "Employee ID")]
    [StringLength(50)]
    public string? EmployeeId { get; set; }
}

