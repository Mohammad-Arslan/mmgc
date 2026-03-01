namespace MMGC.Shared.DTOs;

/// <summary>
/// Represents a dashboard item (appointment, prescription, lab test, etc.) for the patient dashboard.
/// </summary>
public class DashboardItemDto
{
    /// <summary>
    /// Unique identifier for the item.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Type of dashboard item (Appointment, Prescription, LabTest, Invoice, etc.).
    /// </summary>
    public string ItemType { get; set; } = string.Empty;

    /// <summary>
    /// Title or summary of the item.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Detailed description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Current status of the item.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Date when this item was created or scheduled.
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Optional: Due date or completion date.
    /// </summary>
    public DateTime? DueDate { get; set; }

    /// <summary>
    /// Associated doctor or provider name.
    /// </summary>
    public string? ProviderName { get; set; }

    /// <summary>
    /// URL to view or download details.
    /// </summary>
    public string? ActionUrl { get; set; }

    /// <summary>
    /// Whether this item requires action from the patient.
    /// </summary>
    public bool RequiresAction { get; set; }

    /// <summary>
    /// Optional icon class or identifier for UI rendering.
    /// </summary>
    public string? IconClass { get; set; }
}

/// <summary>
/// Complete patient dashboard data.
/// </summary>
public class PatientDashboardDto
{
    /// <summary>
    /// Patient's unique identifier.
    /// </summary>
    public int PatientId { get; set; }

    /// <summary>
    /// Patient's MR (Medical Record) number.
    /// </summary>
    public string MRNumber { get; set; } = string.Empty;

    /// <summary>
    /// Patient's full name.
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Patient's date of birth.
    /// </summary>
    public DateTime DateOfBirth { get; set; }

    /// <summary>
    /// Patient's contact number.
    /// </summary>
    public string? ContactNumber { get; set; }

    /// <summary>
    /// Patient's email address.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Date of last visit.
    /// </summary>
    public DateTime? LastVisitDate { get; set; }

    // ===== SUMMARY COUNTS =====
    /// <summary>
    /// Count of upcoming appointments.
    /// </summary>
    public int UpcomingAppointmentsCount { get; set; }

    /// <summary>
    /// Count of outstanding invoices.
    /// </summary>
    public int OutstandingInvoicesCount { get; set; }

    /// <summary>
    /// Total outstanding amount.
    /// </summary>
    public decimal OutstandingAmount { get; set; }

    /// <summary>
    /// Count of pending test results.
    /// </summary>
    public int PendingLabTestsCount { get; set; }

    // ===== PAGINATED SECTIONS =====
    /// <summary>
    /// Upcoming appointments (next 5).
    /// </summary>
    public List<DashboardItemDto> UpcomingAppointments { get; set; } = new();

    /// <summary>
    /// Recent prescriptions.
    /// </summary>
    public List<DashboardItemDto> RecentPrescriptions { get; set; } = new();

    /// <summary>
    /// Recent lab tests.
    /// </summary>
    public List<DashboardItemDto> RecentLabTests { get; set; } = new();

    /// <summary>
    /// Outstanding invoices.
    /// </summary>
    public List<DashboardItemDto> OutstandingInvoices { get; set; } = new();

    /// <summary>
    /// Notification preferences flag.
    /// </summary>
    public bool NotificationsEnabled { get; set; } = true;

    /// <summary>
    /// Timestamp of when this dashboard was last refreshed.
    /// </summary>
    public DateTime LastRefreshed { get; set; }
}
