using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MMGC.Data;
using MMGC.Models;
using MMGC.Shared.DTOs;
using MMGC.Shared.Interfaces;

namespace MMGC.Shared.Infrastructure.Services;

/// <summary>
/// Service for doctor directory with public-facing information.
/// Uses projection queries and AsNoTracking for performance.
/// </summary>
public class DoctorDirectoryService : IDoctorDirectoryService
{
    private readonly ApplicationDbContext _context;
    private readonly IAvailabilityService _availabilityService;
    private readonly ILogger<DoctorDirectoryService> _logger;

    public DoctorDirectoryService(
        ApplicationDbContext context,
        IAvailabilityService availabilityService,
        ILogger<DoctorDirectoryService> logger)
    {
        _context = context;
        _availabilityService = availabilityService;
        _logger = logger;
    }

    public async Task<(List<DoctorDirectoryDto> Doctors, int TotalCount)> GetDoctorsAsync(
        string? searchTerm = null,
        string? specialization = null,
        int pageNumber = 1,
        int pageSize = 12,
        string sortBy = "name",
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.Doctors
                .AsNoTracking()
                .Where(d => d.IsActive);

            // Filter by search term
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var searchLower = searchTerm.ToLower();
                query = query.Where(d =>
                    d.FirstName.ToLower().Contains(searchLower) ||
                    d.LastName.ToLower().Contains(searchLower) ||
                    d.Specialization.ToLower().Contains(searchLower));
            }

            // Filter by specialization
            if (!string.IsNullOrWhiteSpace(specialization))
            {
                query = query.Where(d => d.Specialization == specialization);
            }

            // Sort
            query = sortBy.ToLower() switch
            {
                "fee" => query.OrderBy(d => d.ConsultationFee),
                "rating" => query.OrderByDescending(d => d.Id), // Placeholder - integrate actual ratings
                _ => query.OrderBy(d => d.FirstName).ThenBy(d => d.LastName)
            };

            var totalCount = await query.CountAsync(cancellationToken);

            var doctors = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(d => new DoctorDirectoryDto
                {
                    Id = d.Id,
                    FirstName = d.FirstName,
                    LastName = d.LastName,
                    Specialization = d.Specialization,
                    Email = d.Email,
                    ContactNumber = d.ContactNumber,
                    LicenseNumber = d.LicenseNumber,
                    ConsultationFee = d.ConsultationFee,
                    IsActive = d.IsActive,
                    TotalAppointmentsCompleted = d.Appointments.Count(a => a.Status == "Completed"),
                    AverageRating = 4.5, // TODO: Implement actual rating calculation
                    AvailableSlotsCount = d.Schedules.Count(ds => ds.IsAvailable)
                })
                .ToListAsync(cancellationToken);

            return (doctors, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching doctors");
            throw;
        }
    }

    public async Task<List<DoctorCardDto>> GetFeaturedDoctorsAsync(
        int count = 6,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var doctors = await _context.Doctors
                .AsNoTracking()
                .Where(d => d.IsActive)
                .OrderByDescending(d => d.Appointments.Count)
                .Take(count)
                .Select(d => new DoctorCardDto
                {
                    Id = d.Id,
                    FirstName = d.FirstName,
                    LastName = d.LastName,
                    Specialization = d.Specialization,
                    ConsultationFee = d.ConsultationFee,
                    AverageRating = 4.5, // TODO: Implement actual rating
                    PatientCount = d.Appointments.Select(a => a.PatientId).Distinct().Count()
                })
                .ToListAsync(cancellationToken);

            return doctors;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching featured doctors");
            throw;
        }
    }

    public async Task<DoctorProfileDto?> GetDoctorProfileAsync(
        int doctorId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var doctor = await _context.Doctors
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == doctorId && d.IsActive, cancellationToken);

            if (doctor == null)
                return null;

            // Get all slots with status (Available, Booked, Past) for next 7 days
            var startDate = DateTime.Today;
            var endDate = DateTime.Today.AddDays(7);
            var allSlots = await _availabilityService.GetSlotsWithStatusInRangeAsync(
                doctorId, startDate, endDate, cancellationToken);
            var availableCount = allSlots.Count(s => s.SlotStatus == Shared.Enums.SlotStatusEnum.Available);

            return new DoctorProfileDto
            {
                Id = doctor.Id,
                FirstName = doctor.FirstName,
                LastName = doctor.LastName,
                Specialization = doctor.Specialization,
                Email = doctor.Email,
                ContactNumber = doctor.ContactNumber,
                LicenseNumber = doctor.LicenseNumber,
                ConsultationFee = doctor.ConsultationFee,
                IsActive = doctor.IsActive,
                CreatedDate = doctor.CreatedDate,
                Address = doctor.Address,
                Biography = null, // TODO: Add biography field to Doctor model if needed
                TotalAppointmentsCompleted = doctor.Appointments.Count(a => a.Status == "Completed"),
                AverageRating = 4.5, // TODO: Implement actual rating
                AvailableSlotsCount = availableCount,
                AvailableSlots = allSlots
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching doctor profile for ID {DoctorId}", doctorId);
            throw;
        }
    }

    public async Task<List<string>> GetSpecializationsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var specializations = await _context.Doctors
                .AsNoTracking()
                .Where(d => d.IsActive)
                .Select(d => d.Specialization)
                .Distinct()
                .OrderBy(s => s)
                .ToListAsync(cancellationToken);

            return specializations;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching specializations");
            throw;
        }
    }

    public async Task<double> GetDoctorRatingAsync(int doctorId, CancellationToken cancellationToken = default)
    {
        try
        {
            var rating = await _context.Testimonials
                .AsNoTracking()
                .Where(t => t.DoctorId == doctorId && t.IsApproved)
                .AverageAsync(t => (double)t.Rating, cancellationToken);

            return rating > 0 ? Math.Round(rating, 1) : 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating doctor rating for ID {DoctorId}", doctorId);
            return 0;
        }
    }
}

/// <summary>
/// Service for public website functionality (homepage, testimonials, contact, statistics).
/// </summary>
public class PublicWebsiteService : IPublicWebsiteService
{
    private readonly ApplicationDbContext _context;
    private readonly IDoctorDirectoryService _doctorDirectoryService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<PublicWebsiteService> _logger;

    public PublicWebsiteService(
        ApplicationDbContext context,
        IDoctorDirectoryService doctorDirectoryService,
        INotificationService notificationService,
        ILogger<PublicWebsiteService> logger)
    {
        _context = context;
        _doctorDirectoryService = doctorDirectoryService;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<HomepageDto> GetHomepageDataAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var homepage = new HomepageDto
            {
                FeaturedDoctors = await _doctorDirectoryService.GetFeaturedDoctorsAsync(6, cancellationToken),
                Services = await GetServicesAsync(cancellationToken),
                Testimonials = await GetTestimonialsAsync(null, 6, cancellationToken),
                Statistics = await GetStatisticsAsync(cancellationToken)
            };

            return homepage;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching homepage data");
            throw;
        }
    }

    public async Task<List<TestimonialDto>> GetTestimonialsAsync(
        int? doctorId = null,
        int count = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.Testimonials
                .AsNoTracking()
                .Where(t => t.IsApproved);

            if (doctorId.HasValue)
            {
                query = query.Where(t => t.DoctorId == doctorId.Value);
            }

            var testimonials = await query
                .OrderByDescending(t => t.CreatedDate)
                .Take(count)
                .Select(t => new TestimonialDto
                {
                    Id = t.Id,
                    PatientName = t.Patient != null ? t.Patient.FirstName : "Anonymous",
                    DoctorName = t.Doctor != null ? t.Doctor.FullName : "Unknown",
                    Specialization = t.Doctor != null ? t.Doctor.Specialization : "",
                    Message = t.Message,
                    Rating = t.Rating,
                    CreatedDate = t.CreatedDate
                })
                .ToListAsync(cancellationToken);

            return testimonials;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching testimonials");
            throw;
        }
    }

    public async Task<int> SubmitContactMessageAsync(
        string firstName,
        string lastName,
        string email,
        string phoneNumber,
        string subject,
        string message,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var contactMessage = new ContactMessage
            {
                FirstName = firstName,
                LastName = lastName,
                Email = email,
                PhoneNumber = phoneNumber,
                Subject = subject,
                Message = message,
                Status = "New",
                CreatedDate = DateTime.UtcNow
            };

            _context.ContactMessages.Add(contactMessage);
            await _context.SaveChangesAsync(cancellationToken);

            // Send notification to admin
            _ = _notificationService.SendEmailAsync(
                "admin@medicarehospital.com", // TODO: Get from config
                $"New Contact Message: {subject}",
                $"""
                New contact message from {firstName} {lastName}
                Email: {email}
                Phone: {phoneNumber}
                
                Message:
                {message}
                """,
                null,
                cancellationToken);

            return contactMessage.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting contact message");
            throw;
        }
    }

    public async Task<StatisticsDto> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var totalDoctors = await _context.Doctors.CountAsync(d => d.IsActive, cancellationToken);
            var totalPatients = await _context.Patients.CountAsync(cancellationToken);
            var completedAppointments = await _context.Appointments
                .CountAsync(a => a.Status == "Completed", cancellationToken);
            var specializations = await _context.Doctors
                .AsNoTracking()
                .Select(d => d.Specialization)
                .Distinct()
                .CountAsync(cancellationToken);

            var averageRating = await _context.Testimonials
                .AsNoTracking()
                .Where(t => t.IsApproved)
                .AverageAsync(t => (double?)t.Rating, cancellationToken) ?? 0;

            return new StatisticsDto
            {
                TotalDoctors = totalDoctors,
                TotalPatients = totalPatients,
                CompletedAppointments = completedAppointments,
                Specializations = specializations,
                AverageRating = (decimal)averageRating
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating statistics");
            throw;
        }
    }

    private async Task<List<ServiceDto>> GetServicesAsync(CancellationToken cancellationToken = default)
    {
        // TODO: Create Service model and store in database
        // For now, return hardcoded services based on doctor specializations
        var specializations = await _context.Doctors
            .AsNoTracking()
            .Where(d => d.IsActive)
            .Select(d => d.Specialization)
            .Distinct()
            .ToListAsync(cancellationToken);

        return specializations
            .Select((spec, index) => new ServiceDto
            {
                Id = index + 1,
                Name = spec,
                Description = $"Professional {spec} services",
                IconClass = "bi-hospital",
                DoctorsCount = 0 // TODO: Calculate actual count per specialization
            })
            .ToList();
    }
}
