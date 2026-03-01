namespace MMGC.Shared.Constants;

/// <summary>
/// System-wide constants for the MMGC Hospital Management System.
/// </summary>
public static class SystemConstants
{
    // ===== APPOINTMENT SETTINGS =====
    /// <summary>
    /// Standard appointment slot duration in minutes.
    /// </summary>
    public const int APPOINTMENT_SLOT_DURATION_MINUTES = 30;

    /// <summary>
    /// Maximum number of overlapping appointment slots allowed.
    /// </summary>
    public const int MAX_CONCURRENT_PATIENTS_PER_DOCTOR = 1;

    // ===== PAGINATION =====
    /// <summary>
    /// Default page size for list operations.
    /// </summary>
    public const int DEFAULT_PAGE_SIZE = 10;

    /// <summary>
    /// Maximum page size allowed (prevents abuse).
    /// </summary>
    public const int MAX_PAGE_SIZE = 100;

    // ===== FILE UPLOADS =====
    /// <summary>
    /// Maximum file size for report uploads in bytes (10 MB).
    /// </summary>
    public const long MAX_FILE_SIZE_BYTES = 10 * 1024 * 1024;

    /// <summary>
    /// Allowed file extensions for medical reports.
    /// </summary>
    public static readonly string[] ALLOWED_REPORT_EXTENSIONS = { ".pdf", ".jpg", ".jpeg", ".png", ".dcm" };

    // ===== PDF GENERATION =====
    /// <summary>
    /// Hospital name for PDF headers.
    /// </summary>
    public const string HOSPITAL_NAME = "MMGC Hospital";

    /// <summary>
    /// PDF confidentiality notice footer text.
    /// </summary>
    public const string PDF_CONFIDENTIALITY_NOTICE = "This document is confidential and intended for the use of the individual to whom it is addressed. If you are not the named addressee, you should not disseminate, distribute, or copy this document.";

    // ===== NOTIFICATIONS =====
    /// <summary>
    /// Maximum number of notification retry attempts.
    /// </summary>
    public const int MAX_NOTIFICATION_RETRIES = 3;

    /// <summary>
    /// Delay in minutes between notification retry attempts.
    /// </summary>
    public const int NOTIFICATION_RETRY_DELAY_MINUTES = 5;

    /// <summary>
    /// Hours before appointment to send reminder.
    /// </summary>
    public const int APPOINTMENT_REMINDER_HOURS = 24;

    // ===== BUSINESS LOGIC =====
    /// <summary>
    /// Minimum gap in minutes between consecutive appointments for the same doctor.
    /// </summary>
    public const int MIN_APPOINTMENT_GAP_MINUTES = 0;

    /// <summary>
    /// Days before appointment to allow cancellation without penalty.
    /// </summary>
    public const int CANCELLATION_ALLOWED_DAYS_BEFORE = 1;

    /// <summary>
    /// Default currency for transactions.
    /// </summary>
    public const string DEFAULT_CURRENCY = "USD";

    // ===== SEARCH =====
    /// <summary>
    /// Minimum characters required for search query.
    /// </summary>
    public const int MIN_SEARCH_CHARACTERS = 2;

    /// <summary>
    /// Maximum results returned per search category.
    /// </summary>
    public const int MAX_SEARCH_RESULTS_PER_CATEGORY = 20;

    // ===== AUDIT LOGGING =====
    /// <summary>
    /// System user ID for background operations.
    /// </summary>
    public const string SYSTEM_USER_ID = "SYSTEM";

    // ===== TIME ZONES =====
    /// <summary>
    /// Default timezone for the system.
    /// </summary>
    public const string DEFAULT_TIMEZONE = "UTC";

    // ===== ROLE NAMES =====
    public static class Roles
    {
        public const string ADMIN = "Admin";
        public const string DOCTOR = "Doctor";
        public const string PATIENT = "Patient";
        public const string NURSE = "Nurse";
        public const string LAB_STAFF = "LabStaff";
        public const string RECEPTION_STAFF = "ReceptionStaff";
        public const string ACCOUNTS_STAFF = "AccountsStaff";
    }

    // ===== EMAIL TEMPLATES =====
    public static class EmailTemplates
    {
        public const string APPOINTMENT_CONFIRMATION_SUBJECT = "Appointment Confirmation - {0}";
        public const string LAB_REPORT_READY_SUBJECT = "Your Lab Report is Ready";
        public const string INVOICE_GENERATED_SUBJECT = "Invoice {0} - {1}";
        public const string PROCEDURE_APPROVED_SUBJECT = "Your Procedure Request Has Been Approved";
    }
}
