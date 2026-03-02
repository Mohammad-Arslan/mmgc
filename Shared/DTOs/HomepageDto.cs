namespace MMGC.Shared.DTOs;

/// <summary>
/// DTO for hospital homepage content.
/// </summary>
public class HomepageDto
{
    /// <summary>
    /// Featured doctors for carousel.
    /// </summary>
    public List<DoctorCardDto> FeaturedDoctors { get; set; } = new();
    
    /// <summary>
    /// Hospital services offered.
    /// </summary>
    public List<ServiceDto> Services { get; set; } = new();
    
    /// <summary>
    /// Patient testimonials.
    /// </summary>
    public List<TestimonialDto> Testimonials { get; set; } = new();
    
    /// <summary>
    /// Hospital statistics.
    /// </summary>
    public StatisticsDto Statistics { get; set; } = new();
}

/// <summary>
/// DTO for hospital service.
/// </summary>
public class ServiceDto
{
    public int Id { get; set; }
    
    public string Name { get; set; } = string.Empty;
    
    public string Description { get; set; } = string.Empty;
    
    public string? IconClass { get; set; } // Bootstrap icon class (e.g., "bi-hospital")
    
    public int DoctorsCount { get; set; }
}

/// <summary>
/// DTO for patient testimonial.
/// </summary>
public class TestimonialDto
{
    public int Id { get; set; }
    
    public string PatientName { get; set; } = string.Empty;
    
    public string DoctorName { get; set; } = string.Empty;
    
    public string Specialization { get; set; } = string.Empty;
    
    public string Message { get; set; } = string.Empty;
    
    public int Rating { get; set; } // 1-5
    
    public DateTime CreatedDate { get; set; }
}

/// <summary>
/// DTO for hospital statistics.
/// </summary>
public class StatisticsDto
{
    public int TotalDoctors { get; set; }
    
    public int TotalPatients { get; set; }
    
    public int CompletedAppointments { get; set; }
    
    public int Specializations { get; set; }
    
    public decimal AverageRating { get; set; }
}
