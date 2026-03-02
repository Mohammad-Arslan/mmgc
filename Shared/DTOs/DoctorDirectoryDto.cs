namespace MMGC.Shared.DTOs;

/// <summary>
/// DTO for displaying doctor information in public directory listings.
/// </summary>
public class DoctorDirectoryDto
{
    public int Id { get; set; }
    
    public string FirstName { get; set; } = string.Empty;
    
    public string LastName { get; set; } = string.Empty;
    
    public string FullName => $"{FirstName} {LastName}";
    
    public string Specialization { get; set; } = string.Empty;
    
    public string? Email { get; set; }
    
    public string? ContactNumber { get; set; }
    
    public string? LicenseNumber { get; set; }
    
    public decimal ConsultationFee { get; set; }
    
    public bool IsActive { get; set; }
    
    public int TotalAppointmentsCompleted { get; set; }
    
    public double AverageRating { get; set; }
    
    /// <summary>
    /// Count of available appointment slots in the next 7 days.
    /// </summary>
    public int AvailableSlotsCount { get; set; }
    
    /// <summary>
    /// Years of experience (calculated from license or profile data).
    /// </summary>
    public int? YearsOfExperience { get; set; }

    /// <summary>
    /// Profile image URL (from polymorphic Image model).
    /// </summary>
    public string? ProfileImageUrl { get; set; }
}

/// <summary>
/// DTO for doctor profile page with complete information.
/// </summary>
public class DoctorProfileDto : DoctorDirectoryDto
{
    public DateTime CreatedDate { get; set; }
    
    public string? Address { get; set; }
    
    public string? Biography { get; set; }
    
    /// <summary>
    /// List of available appointment slots for the next 7 days.
    /// </summary>
    public List<AppointmentSlotDto> AvailableSlots { get; set; } = new();
}

/// <summary>
/// DTO for doctor quick view in carousel/featured section.
/// </summary>
public class DoctorCardDto
{
    public int Id { get; set; }
    
    public string FirstName { get; set; } = string.Empty;
    
    public string LastName { get; set; } = string.Empty;
    
    public string FullName => $"{FirstName} {LastName}";
    
    public string Specialization { get; set; } = string.Empty;
    
    public decimal ConsultationFee { get; set; }
    
    public double AverageRating { get; set; }
    
    public int PatientCount { get; set; }

    /// <summary>
    /// Profile image URL (from polymorphic Image model).
    /// </summary>
    public string? ProfileImageUrl { get; set; }
}
