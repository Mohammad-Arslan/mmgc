using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MMGC.Data;

namespace MMGC.Models;

/// <summary>
/// Represents a patient testimonial/review for a doctor.
/// </summary>
public class Testimonial
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Patient providing the testimonial.
    /// </summary>
    [Required]
    public int PatientId { get; set; }

    /// <summary>
    /// Doctor being reviewed.
    /// </summary>
    [Required]
    public int DoctorId { get; set; }

    /// <summary>
    /// Patient's rating (1-5 stars).
    /// </summary>
    [Required]
    [Range(1, 5)]
    [Display(Name = "Rating")]
    public int Rating { get; set; }

    /// <summary>
    /// Testimonial message.
    /// </summary>
    [Required]
    [StringLength(1000)]
    [Display(Name = "Testimonial")]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Whether this testimonial is approved for public display.
    /// </summary>
    [Display(Name = "Is Approved")]
    public bool IsApproved { get; set; } = false;

    /// <summary>
    /// Date created.
    /// </summary>
    [Required]
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(PatientId))]
    public virtual Patient? Patient { get; set; }

    [ForeignKey(nameof(DoctorId))]
    public virtual Doctor? Doctor { get; set; }
}

/// <summary>
/// Represents a contact form submission from public website.
/// </summary>
public class ContactMessage
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

    [Required]
    [EmailAddress]
    [StringLength(100)]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(15)]
    [Display(Name = "Phone Number")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    [Display(Name = "Subject")]
    public string Subject { get; set; } = string.Empty;

    [Required]
    [StringLength(2000)]
    [Display(Name = "Message")]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Status of the message (New, In Progress, Resolved, Closed).
    /// </summary>
    [StringLength(20)]
    [Display(Name = "Status")]
    public string Status { get; set; } = "New";

    /// <summary>
    /// Admin notes for internal reference.
    /// </summary>
    [StringLength(1000)]
    [Display(Name = "Admin Notes")]
    public string? AdminNotes { get; set; }

    /// <summary>
    /// Date message was created.
    /// </summary>
    [Required]
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Date message was resolved.
    /// </summary>
    public DateTime? ResolvedDate { get; set; }

    /// <summary>
    /// Admin user ID who resolved the message.
    /// </summary>
    [StringLength(450)]
    public string? ResolvedBy { get; set; }
}
