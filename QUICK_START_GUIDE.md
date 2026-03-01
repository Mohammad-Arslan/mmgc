# Phase 2 Quick Reference & Next Steps

## 🚀 Immediate Actions (Do These First)

### 1. Update Program.cs (5 minutes)

Add these using statements at the top:
```csharp
using MMGC.Shared.Interfaces;
using MMGC.Features.Patients.Services;
using MMGC.Features.Search.Services;
using MMGC.Shared.Infrastructure.Services;
```

Add this registration code after existing services:
```csharp
// ===== PHASE 2: FEATURE SERVICES =====
builder.Services.AddScoped<IPatientDashboardService, PatientDashboardService>();
builder.Services.AddScoped<ISearchService, SearchService>();
builder.Services.AddScoped<INotificationLogService, NotificationLogService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<INotificationProvider, SmsNotificationProvider>();
builder.Services.AddScoped<INotificationProvider, EmailNotificationProvider>();
builder.Services.AddScoped<IAvailabilityService, AvailabilityService>();
builder.Services.AddScoped<IProcedureWorkflowService, ProcedureWorkflowService>();
```

### 2. Update appsettings.json (2 minutes)

Add Twilio configuration:
```json
{
  "Twilio": {
    "AccountSid": "your_account_sid_here",
    "AuthToken": "your_auth_token_here",
    "FromPhone": "+1234567890"
  }
}
```

### 3. Run Database Migration (2 minutes)

```bash
cd MMGC
dotnet ef database update
```

Or if using Package Manager Console in Visual Studio:
```powershell
Update-Database
```

### 4. Verify Build (1 minute)

```bash
dotnet build
```

Should complete with no errors.

---

## 📋 What Was Implemented

### Services Ready to Use

| Service | Location | Purpose |
|---------|----------|---------|
| `IPatientDashboardService` | `Features/Patients/Services/` | Aggregate dashboard data |
| `INotificationService` | `Shared/Infrastructure/Services/` | Send notifications |
| `ISearchService` | `Features/Search/Services/` | Unified search |
| `IAvailabilityService` | `Features/Appointments/Services/` | Manage appointment slots |
| `IProcedureWorkflowService` | `Features/Procedures/Services/` | Procedure approval workflow |

### Example Usage in Controllers

```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PatientDashboardController : ControllerBase
{
    private readonly IPatientDashboardService _dashboardService;

    public PatientDashboardController(IPatientDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet("{patientId}")]
    public async Task<IActionResult> GetDashboard(int patientId)
    {
        var dashboard = await _dashboardService.GetPatientDashboardAsync(patientId);
        return Ok(dashboard);
    }
}
```

---

## 🎯 Phase 2C: Next Features to Build

### Controllers to Create

1. **PatientDashboardController**
   - GET /dashboard/{patientId}
   - GET /appointments/{patientId}
   - GET /prescriptions/{patientId}
   - GET /lab-tests/{patientId}
   - GET /invoices/{patientId}

2. **SearchController**
   - GET /search?q={query}
   - GET /search/doctors?q={query}&specialization={spec}
   - GET /search/patients?q={query}
   - GET /search/procedures?q={query}

3. **AppointmentsController** (Enhance)
   - GET /available-slots/{doctorId}/{date}
   - POST /book-slot (with availability check)

4. **ProceduresController** (Enhance)
   - POST /request (create procedure request)
   - GET /pending (for doctors)
   - POST /approve/{id} (doctor approval)
   - POST /reject/{id} (doctor rejection)

5. **ReportsController** (New)
   - GET /prescription/{prescriptionId}/pdf
   - GET /invoice/{transactionId}/pdf
   - GET /lab-report/{labTestId}/pdf

### Razor Pages to Create

1. **Dashboard.cshtml**
   - Display patient summary
   - Show upcoming appointments
   - Quick action buttons
   - Links to history pages

2. **SearchResults.cshtml**
   - Display grouped results
   - Doctor cards with filters
   - Patient search results

3. **AppointmentSlots.cshtml**
   - Calendar view of available slots
   - Slot selection UI
   - Confirmation

4. **ProcedureRequest.cshtml**
   - Form for requesting procedure
   - Request status display
   - Doctor approval interface

---

## 🔑 Key Interfaces Reference

### IPatientDashboardService
```csharp
Task<PatientDashboardDto> GetPatientDashboardAsync(int patientId);
Task<(List<DashboardItemDto> Items, int TotalCount)> GetAppointmentHistoryAsync(int patientId, int pageNumber, int pageSize);
// ... 4 more history methods
```

### INotificationService
```csharp
Task<string> SendAsync(NotificationMessageDto message);
Task<string> SendSmsAsync(string phone, string message, int? patientId);
Task<List<string>> SendAppointmentConfirmationAsync(int appointmentId);
Task<List<string>> SendAppointmentReminderAsync(int appointmentId);
Task<List<string>> SendProcedureApprovedAsync(int procedureRequestId);
// ... more methods
```

### ISearchService
```csharp
Task<List<GroupedSearchResultDto>> SearchAsync(string query);
Task<List<SearchResultDto>> SearchDoctorsAsync(string query, string? specialization);
Task<List<SearchResultDto>> SearchPatientsAsync(string query);
Task<List<SearchResultDto>> SearchProceduresAsync(string query);
```

### IAvailabilityService
```csharp
Task<List<AppointmentSlotDto>> GetAvailableSlotsAsync(int doctorId, DateTime date);
Task<int> ReserveSlotAsync(int doctorId, int patientId, DateTime startTime, DateTime endTime);
Task<bool> IsSlotAvailableAsync(int doctorId, DateTime startTime, DateTime endTime);
```

### IProcedureWorkflowService
```csharp
Task<ProcedureRequestDto> CreateProcedureRequestAsync(int patientId, string type, string reason);
Task<ProcedureRequestDto> ApproveProcedureRequestAsync(int requestId, int doctorId);
Task<ProcedureRequestDto> RejectProcedureRequestAsync(int requestId, int doctorId, string reason);
Task<int> ScheduleApprovedProcedureAsync(int requestId, DateTime scheduledDateTime);
```

---

## 📊 Database Schema Summary

### New Tables

**ProcedureRequest**
- Id, PatientId, DoctorId, ProcedureType, Status, CreatedDate, ReviewedDate, LinkedProcedureId

**NotificationLog**
- Id, NotificationId, RecipientContact, NotificationType, Status, CreatedAt, DeliveredAt, RetryCount

**DocumentAuditLog**
- Id, DocumentType, EntityId, PatientId, RequestedBy, FileName, GeneratedAt, Status

### Enhanced Tables

**Appointment**
- Added: StatusEnum, AppointmentEndTime, RowVersion (concurrency), CreatedBy

---

## 🔒 Authorization Examples

```csharp
// Patient can only see own dashboard
[Authorize(Roles = "Patient")]
[HttpGet("{patientId}")]
public async Task<IActionResult> GetMyDashboard(int patientId)
{
    // Verify patientId matches current user
    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    // ... authorization check ...
}

// Doctor can approve procedures
[Authorize(Roles = "Doctor")]
[HttpPost("approve/{requestId}")]
public async Task<IActionResult> ApproveProcedure(int requestId)
{
    var doctorId = GetDoctorIdFromUser(); // Get from claims
    // ...
}

// Staff can view all searches
[Authorize(Roles = "Staff,Admin")]
[HttpGet("search")]
public async Task<IActionResult> Search(string q)
{
    // ...
}
```

---

## 🧪 Testing Sample Code

```csharp
[TestClass]
public class PatientDashboardServiceTests
{
    private Mock<ApplicationDbContext> _mockContext;
    private PatientDashboardService _service;

    [TestInitialize]
    public void Setup()
    {
        _mockContext = new Mock<ApplicationDbContext>();
        var logger = new Mock<ILogger<PatientDashboardService>>();
        _service = new PatientDashboardService(_mockContext.Object, logger.Object);
    }

    [TestMethod]
    [ExpectedException(typeof(EntityNotFoundException))]
    public async Task GetDashboard_InvalidPatientId_ThrowsException()
    {
        await _service.GetPatientDashboardAsync(999);
    }
}
```

---

## 📝 Notificationflow Example

```csharp
// Appointment booked → Auto-send confirmation
var appointmentId = await appointmentService.CreateAsync(/* ... */);
await notificationService.SendAppointmentConfirmationAsync(appointmentId);

// Notification logged to database
// Sent via SMS/Email in background
// Status tracked in NotificationLog table
// Can be retrieved: await notificationService.GetNotificationStatusAsync(id);

// Reminders sent 24 hours before
// Failed notifications retried automatically
// Old logs cleaned up after 90 days
```

---

## 🎯 Configuration Defaults

From SystemConstants.cs:

| Setting | Value |
|---------|-------|
| Slot Duration | 30 minutes |
| Max Patients/Doctor | 1 (no overlapping) |
| Page Size | 10 items |
| Max Page Size | 100 items |
| File Size Limit | 10 MB |
| Notification Retries | 3 attempts |
| Retry Delay | 5 minutes |
| Appointment Reminder | 24 hours before |

---

## ✅ Pre-Launch Checklist

- [ ] Program.cs updated with service registrations
- [ ] appsettings.json updated with Twilio credentials
- [ ] Database migration applied successfully
- [ ] Code compiles without errors
- [ ] Services can be injected in controllers
- [ ] Notification sending works manually
- [ ] Search functionality tested
- [ ] Dashboard aggregation verified
- [ ] Slot availability checked
- [ ] Procedure workflow validated

---

## 📞 Common Issues & Solutions

### Issue: "Service not registered" error
**Solution**: Ensure service is registered in Program.cs with correct interface

### Issue: "NotificationId unique constraint failed"
**Solution**: NotificationIds must be unique; system uses Guid - should not happen

### Issue: "Appointment reservation failed - slot already booked"
**Solution**: Race condition in high concurrency; RowVersion concurrency control prevents this

### Issue: "Twilio service not responding"
**Solution**: Check appsettings.json Twilio config, verify credentials

### Issue: "Email not sent"
**Solution**: Email provider is stubbed; implement SendGrid integration

---

## 🔗 Service Dependencies

```
PatientDashboardService
  └─ ApplicationDbContext

NotificationService
  ├─ ApplicationDbContext
  ├─ INotificationLogService
  └─ INotificationProvider[] (SMS, Email)

SearchService
  └─ ApplicationDbContext

AvailabilityService
  └─ ApplicationDbContext

ProcedureWorkflowService
  ├─ ApplicationDbContext
  └─ INotificationService
```

All services follow dependency injection pattern. No circular dependencies.

---

## 📞 Contact Points

For each feature, you can contact:

**Notifications**: Check NotificationLog table for delivery status
**Search**: Use advanced filters for specific entity types
**Dashboard**: Call GetPatientDashboardAsync for complete summary
**Availability**: Call GetAvailableSlotsAsync for date range
**Procedures**: Use workflow service for state management

---

## 🎓 Architecture Patterns Used

1. **Dependency Injection** - Constructor-based, fully mockable
2. **Repository Pattern** - EF Core as repository
3. **Data Transfer Objects** - Entities never exposed to UI
4. **Async/Await** - All I/O operations non-blocking
5. **Fire-and-Forget** - Notifications don't block requests
6. **Strategy Pattern** - Multiple notification providers
7. **Pagination** - All lists support paging
8. **Concurrency Control** - RowVersion prevents race conditions
9. **Audit Logging** - All critical operations logged
10. **Exception Hierarchy** - Specific exceptions for each scenario

---

## 🚀 You're Ready!

All foundations are in place. The system is ready to:

1. ✅ Register services
2. ✅ Run migrations
3. ✅ Build controllers
4. ✅ Create views
5. ✅ Test features
6. ✅ Deploy to production

**Estimated time to completion: 8-10 hours for Phase 2C**

Start with PatientDashboardController and work your way down.

Good luck! 🎉

