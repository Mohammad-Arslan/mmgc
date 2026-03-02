using Microsoft.AspNetCore.Mvc;
using MMGC.Shared.Interfaces;

namespace MMGC.Controllers;

/// <summary>
/// Controller for public website features (homepage, doctors directory, contact form).
/// Accessible without authentication.
/// Default landing page for the application.
/// </summary>
public class PublicController : Controller
{
    private readonly IPublicWebsiteService _publicWebsiteService;
    private readonly IDoctorDirectoryService _doctorDirectoryService;
    private readonly ILogger<PublicController> _logger;

    public PublicController(
        IPublicWebsiteService publicWebsiteService,
        IDoctorDirectoryService doctorDirectoryService,
        ILogger<PublicController> logger)
    {
        _publicWebsiteService = publicWebsiteService;
        _doctorDirectoryService = doctorDirectoryService;
        _logger = logger;
    }

    /// <summary>
    /// Homepage with featured doctors, services, and testimonials.
    /// This is the default landing page of the application.
    /// Route: / or /public or /public/index
    /// </summary>
    [HttpGet("/")]
    [HttpGet("/public")]
    [HttpGet("/public/index")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Loading homepage...");
            var homepage = await _publicWebsiteService.GetHomepageDataAsync(cancellationToken);
            _logger.LogInformation("Homepage loaded successfully");
            return View(homepage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error loading homepage: {ex.Message}");
            _logger.LogError($"Inner Exception: {ex.InnerException?.Message}");
            _logger.LogError($"Stack Trace: {ex.StackTrace}");
            return BadRequest($"Unable to load homepage: {ex.Message}");
        }
    }

    /// <summary>
    /// Doctors directory with filtering and search.
    /// Route: /public/doctors or /doctors
    /// </summary>
    [HttpGet("/public/doctors")]
    [HttpGet("/doctors")]
    public async Task<IActionResult> Doctors(
        string? search = null,
        string? specialization = null,
        int page = 1,
        string sortBy = "name",
        CancellationToken cancellationToken = default)
    {
        try
        {
            const int pageSize = 12;
            var (doctors, totalCount) = await _doctorDirectoryService.GetDoctorsAsync(
                search, specialization, page, pageSize, sortBy, cancellationToken);

            var specializations = await _doctorDirectoryService.GetSpecializationsAsync(cancellationToken);

            ViewBag.Search = search;
            ViewBag.Specialization = specialization;
            ViewBag.SortBy = sortBy;
            ViewBag.Specializations = specializations;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalCount / pageSize);
            ViewBag.TotalCount = totalCount;

            return View(doctors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading doctors directory");
            return BadRequest("Unable to load doctors directory");
        }
    }

    /// <summary>
    /// Individual doctor profile with available appointment slots.
    /// Route: /public/doctors/{doctorId} or /doctors/{doctorId}
    /// </summary>
    [HttpGet("/public/doctors/{doctorId}")]
    [HttpGet("/doctors/{doctorId}")]
    public async Task<IActionResult> DoctorProfile(
        int doctorId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var profile = await _doctorDirectoryService.GetDoctorProfileAsync(doctorId, cancellationToken);
            if (profile == null)
                return NotFound("Doctor not found");

            return View(profile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading doctor profile for ID {DoctorId}", doctorId);
            return BadRequest("Unable to load doctor profile");
        }
    }

    /// <summary>
    /// Contact form submission.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Contact(
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
            if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName) ||
                string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(message))
            {
                return BadRequest("All fields are required");
            }

            var messageId = await _publicWebsiteService.SubmitContactMessageAsync(
                firstName, lastName, email, phoneNumber, subject, message, cancellationToken);

            return Ok(new { success = true, messageId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting contact form");
            return StatusCode(500, new { success = false, error = "Unable to submit message" });
        }
    }
}
