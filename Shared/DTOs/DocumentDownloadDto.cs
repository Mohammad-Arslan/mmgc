namespace MMGC.Shared.DTOs;

/// <summary>
/// DTO for contact form submission.
/// </summary>
public class ContactMessageDto
{
    public int Id { get; set; }
    
    public string FirstName { get; set; } = string.Empty;
    
    public string LastName { get; set; } = string.Empty;
    
    public string Email { get; set; } = string.Empty;
    
    public string PhoneNumber { get; set; } = string.Empty;
    
    public string Subject { get; set; } = string.Empty;
    
    public string Message { get; set; } = string.Empty;
    
    public string Status { get; set; } = "New"; // New, In Progress, Resolved, Closed
    
    public DateTime CreatedDate { get; set; }
    
    public DateTime? ResolvedDate { get; set; }
    
    public string? AdminNotes { get; set; }
}

/// <summary>
/// DTO for patient invoice/receipt view.
/// </summary>
public class InvoiceDto
{
    public int Id { get; set; }
    
    public int TransactionId { get; set; }
    
    public int PatientId { get; set; }
    
    public string PatientName { get; set; } = string.Empty;
    
    public string PatientMRNumber { get; set; } = string.Empty;
    
    public string TransactionType { get; set; } = string.Empty;
    
    public string Description { get; set; } = string.Empty;
    
    public decimal Amount { get; set; }
    
    public string PaymentMode { get; set; } = string.Empty;
    
    public string? ReferenceNumber { get; set; }
    
    public string Status { get; set; } = string.Empty;
    
    public DateTime TransactionDate { get; set; }
    
    public string? RelatedService { get; set; } // Doctor name, Lab test name, Procedure name, etc.
}

/// <summary>
/// DTO for lab report view.
/// </summary>
public class LabReportDto
{
    public int Id { get; set; }
    
    public int PatientId { get; set; }
    
    public string TestName { get; set; } = string.Empty;
    
    public string CategoryName { get; set; } = string.Empty;
    
    public DateTime TestDate { get; set; }
    
    public string Status { get; set; } = string.Empty;
    
    public DateTime? ApprovedDate { get; set; }
    
    public string? ApprovedByDoctor { get; set; }
    
    public string? ReportNotes { get; set; }
    
    public bool IsApproved { get; set; }
    
    public string? ReportFilePath { get; set; }
    
    public bool CanDownload => IsApproved && !string.IsNullOrEmpty(ReportFilePath);
}

/// <summary>
/// DTO for prescription download.
/// </summary>
public class PrescriptionDto
{
    public int Id { get; set; }
    
    public int PatientId { get; set; }
    
    public string DoctorName { get; set; } = string.Empty;
    
    public DateTime PrescriptionDate { get; set; }
    
    public string PrescriptionDetails { get; set; } = string.Empty;
    
    public DateTime? ValidUntil { get; set; }
}
