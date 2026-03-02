using MMGC.Shared.DTOs;

namespace MMGC.Shared.Interfaces;

/// <summary>
/// Service contract for doctor directory and public-facing doctor information.
/// </summary>
public interface IDoctorDirectoryService
{
    /// <summary>
    /// Gets paginated list of active doctors with filtering and sorting.
    /// </summary>
    Task<(List<DoctorDirectoryDto> Doctors, int TotalCount)> GetDoctorsAsync(
        string? searchTerm = null,
        string? specialization = null,
        int pageNumber = 1,
        int pageSize = 12,
        string sortBy = "name", // name, rating, fee
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets featured doctors for homepage carousel.
    /// </summary>
    Task<List<DoctorCardDto>> GetFeaturedDoctorsAsync(
        int count = 6,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets complete doctor profile with available slots.
    /// </summary>
    Task<DoctorProfileDto?> GetDoctorProfileAsync(
        int doctorId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all unique specializations for filtering.
    /// </summary>
    Task<List<string>> GetSpecializationsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets doctor rating based on testimonials.
    /// </summary>
    Task<double> GetDoctorRatingAsync(int doctorId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Service contract for public website content and information.
/// </summary>
public interface IPublicWebsiteService
{
    /// <summary>
    /// Gets complete homepage data including featured doctors, services, and statistics.
    /// </summary>
    Task<HomepageDto> GetHomepageDataAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets list of approved patient testimonials.
    /// </summary>
    Task<List<TestimonialDto>> GetTestimonialsAsync(
        int? doctorId = null,
        int count = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Submits a contact form message.
    /// </summary>
    Task<int> SubmitContactMessageAsync(
        string firstName,
        string lastName,
        string email,
        string phoneNumber,
        string subject,
        string message,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets hospital statistics for display.
    /// </summary>
    Task<StatisticsDto> GetStatisticsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Service contract for patient document downloads and viewing.
/// </summary>
public interface IDocumentDownloadService
{
    /// <summary>
    /// Gets paginated list of invoices for authenticated patient.
    /// </summary>
    Task<(List<InvoiceDto> Invoices, int TotalCount)> GetPatientInvoicesAsync(
        int patientId,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets paginated list of lab reports for authenticated patient.
    /// </summary>
    Task<(List<LabReportDto> Reports, int TotalCount)> GetPatientLabReportsAsync(
        int patientId,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets paginated list of prescriptions for authenticated patient.
    /// </summary>
    Task<(List<PrescriptionDto> Prescriptions, int TotalCount)> GetPatientPrescriptionsAsync(
        int patientId,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets specific invoice details.
    /// Must verify patient ownership for security.
    /// </summary>
    Task<InvoiceDto?> GetInvoiceAsync(int invoiceId, int patientId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets specific lab report details.
    /// Must verify patient ownership for security.
    /// </summary>
    Task<LabReportDto?> GetLabReportAsync(int labTestId, int patientId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets specific prescription details.
    /// Must verify patient ownership for security.
    /// </summary>
    Task<PrescriptionDto?> GetPrescriptionAsync(int prescriptionId, int patientId, CancellationToken cancellationToken = default);
}
