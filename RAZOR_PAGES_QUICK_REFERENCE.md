# 🚀 Phase 2C Quick Start - Razor Pages Guide

## 📍 New Pages Created

### 1. Patient Dashboard
**Path:** `Pages/Patient/Dashboard.cshtml`
```
Route: /patient/dashboard
Query: ?patientId=1
Auth: [Authorize(Roles = "Patient")]
```

**What it shows:**
- Patient profile info
- Quick stats (4 cards with metrics)
- Upcoming appointments (5 most recent)
- Pending lab tests
- Recent prescriptions
- Outstanding invoices
- Quick action buttons

**Usage:**
```html
<!-- In navigation -->
<a href="/patient/dashboard?patientId=@userId">Dashboard</a>
```

---

### 2. Search Results
**Path:** `Pages/Search/Results.cshtml`
```
Route: /search/results
Query: ?query=doctor&entityType=doctor&specialization=Cardiology
Auth: [Authorize]
```

**What it shows:**
- Search form with filters
- Grouped results by type (Doctors, Procedures, etc.)
- Relevance scores with progress bars
- Result cards with details
- Links to entity details pages

**Usage:**
```html
<!-- In search form -->
<form method="get" action="/search/results">
    <input name="query" placeholder="Search..." />
    <input name="entityType" value="doctor" />
    <button type="submit">Search</button>
</form>
```

---

### 3. Appointment Slots
**Path:** `Pages/Appointments/Slots.cshtml`
```
Route: /appointments/slots
Query: ?doctorId=1&selectedDate=2025-01-20
Auth: [Authorize(Roles = "Patient")]
```

**What it shows:**
- Doctor information
- Date picker (30 days forward)
- Available time slots as radio buttons
- Reason for visit textarea
- Terms agreement checkbox
- Booking confirmation

**Usage:**
```html
<!-- Book appointment link -->
<a href="/appointments/slots?doctorId=@doctorId">Book Appointment</a>
```

---

### 4. Procedure Request
**Path:** `Pages/Procedures/Request.cshtml`
```
Route: /procedures/request
Auth: [Authorize(Roles = "Patient")]
```

**What it shows:**
- Procedure type dropdown
- Reason textarea (min 10 chars)
- Preferred date picker
- Medical history disclaimer
- Consent checkbox
- How-it-works guide
- FAQ accordion

**Usage:**
```html
<!-- Request procedure link -->
<a href="/procedures/request">Request Procedure</a>
```

---

## 🔧 Integration Examples

### Dashboard Integration
```csharp
// In any controller/page
public class MyPageModel : PageModel
{
    private readonly IPatientDashboardService _service;
    
    public async Task OnGetAsync()
    {
        var dashboard = await _service.GetPatientDashboardAsync(patientId);
    }
}
```

### Search Integration
```csharp
// From your page
var results = await _searchService.SearchAsync(query);
var doctors = await _searchService.SearchDoctorsAsync(query, specialization);
```

### Booking Integration
```csharp
// Check availability
var slots = await _availabilityService.GetAvailableSlotsAsync(doctorId, date);

// Reserve slot
var appointmentId = await _availabilityService.ReserveSlotAsync(
    doctorId, patientId, startTime, endTime);
```

---

## 📋 Features by Page

### Dashboard Page
✅ Profile display
✅ 4 quick stat cards
✅ 4 data sections (appointments, prescriptions, lab tests, invoices)
✅ Pagination support (via AJAX handlers)
✅ Quick action buttons
✅ Error handling
✅ Last visit tracking

### Search Page
✅ Unified search box
✅ Multi-filter support
✅ Grouped results
✅ Relevance scoring
✅ "View all" pagination
✅ Entity type icons
✅ Direct links to details

### Slots Page
✅ Date picker with validation
✅ Time slot selection
✅ Available slots listing
✅ Reason for visit validation
✅ Consent agreement
✅ Booking confirmation
✅ Success redirect

### Procedure Request Page
✅ Procedure dropdown (12 types)
✅ Detailed reason textarea
✅ Date preference
✅ Medical history reminder
✅ Consent agreement
✅ How-it-works guide
✅ FAQ section
✅ Request ID on success

---

## 🎯 Authentication & Authorization

All pages are secured:

```csharp
// Patient Dashboard - Patients only
[Authorize(Roles = "Patient")]

// Search - Any authenticated user
[Authorize]

// Appointment Slots - Patients only
[Authorize(Roles = "Patient")]

// Procedure Request - Patients only
[Authorize(Roles = "Patient")]
```

---

## 🧩 Service Dependencies

### Dashboard PageModel
```csharp
IPatientDashboardService _dashboardService
ILogger<DashboardModel> _logger
```

### Search PageModel
```csharp
ISearchService _searchService
ILogger<ResultsModel> _logger
```

### Slots PageModel
```csharp
IAvailabilityService _availabilityService
ILogger<SlotsModel> _logger
```

### Procedure PageModel
```csharp
IProcedureWorkflowService _workflowService
INotificationService _notificationService
ILogger<RequestModel> _logger
```

---

## 📱 Responsive Breakpoints

All pages use Bootstrap 5 responsive grid:

| Device | Breakpoint | Columns |
|--------|-----------|---------|
| Mobile | xs | 1 col |
| Tablet | md | 2 cols |
| Desktop | lg | 3-4 cols |
| Wide | xl | 4+ cols |

---

## 🎨 Styling & Icons

### Bootstrap Classes Used
- `.card` - Content containers
- `.alert` - Messages
- `.badge` - Status labels
- `.btn` - Buttons
- `.form-control` - Form inputs
- `.progress` - Progress bars

### Bootstrap Icons (bi-*)
```html
<i class="bi bi-speedometer2"></i> <!-- Dashboard -->
<i class="bi bi-search"></i> <!-- Search -->
<i class="bi bi-calendar-plus"></i> <!-- Appointment -->
<i class="bi bi-file-earmark-plus"></i> <!-- Procedure -->
```

---

## 🔄 Data Flow

```
User clicks link
    ↓
Page loads with [Authorize] check
    ↓
PageModel OnGetAsync() executes
    ↓
Service methods called (async/await)
    ↓
Data populates DTO objects
    ↓
Razor page renders HTML
    ↓
User sees results
```

---

## 🛠️ Common Customizations

### Change Dashboard Title
```html
<!-- In Dashboard.cshtml -->
<h1 class="h3 mb-0">
    <i class="bi bi-speedometer2"></i> My Health Dashboard
</h1>
```

### Add New Quick Action
```html
<!-- In Dashboard.cshtml Quick Actions section -->
<a href="/new-path" class="btn btn-info btn-sm">
    <i class="bi bi-icon-name"></i> New Action
</a>
```

### Customize Search Form
```csharp
<!-- In Results.cshtml.cs -->
[BindProperty(SupportsGet = true)]
public string? NewFilter { get; set; }
```

### Add Validation Message
```csharp
// In PageModel
if (string.IsNullOrEmpty(Input))
{
    ErrorMessage = "Custom error message";
}
```

---

## 📊 Performance Notes

### Database Queries
- Uses `AsNoTracking()` for read-only queries
- Includes related entities with `Include()`
- Limits results with `Take(n)`
- Implements pagination with `Skip()`/`Take()`

### Caching Opportunity
```csharp
// Future: Add response caching
[ResponseCache(Duration = 300)]
public async Task OnGetAsync()
{
    // Dashboard data cached for 5 minutes
}
```

### Async Throughout
- All database calls are async
- All page handlers are async
- No blocking operations

---

## 🧪 Testing Examples

### Unit Test Dashboard Load
```csharp
[TestMethod]
public async Task Dashboard_OnGet_LoadsPatientData()
{
    var mockService = new Mock<IPatientDashboardService>();
    mockService.Setup(s => s.GetPatientDashboardAsync(1))
        .ReturnsAsync(new PatientDashboardDto());
    
    var model = new DashboardModel(mockService.Object, logger);
    await model.OnGetAsync();
    
    Assert.IsNotNull(model.Dashboard);
}
```

### Test Search Filtering
```csharp
[TestMethod]
public async Task Search_WithDoctorFilter_ReturnsDoctorsOnly()
{
    var model = new ResultsModel(searchService, logger);
    model.Query = "cardio";
    model.EntityType = "doctor";
    
    await model.OnGetAsync();
    
    Assert.IsTrue(model.FilteredResults.All(r => r.EntityType == "Doctor"));
}
```

---

## 🐛 Troubleshooting

### Page not found (404)
- Check route: `/patient/dashboard`
- Verify .cshtml file exists in correct folder
- Check PageModel class name matches

### "Unauthorized" error
- User needs correct role: `[Authorize(Roles = "Patient")]`
- Check user's role in database
- Verify authentication is working

### Service injection error
- Service must be registered in `Program.cs`
- Constructor parameter must match interface name
- Check spelling and using statements

### Data not showing
- Check service is returning data
- Verify database has records
- Check query/filter parameters
- Look at browser console for errors

---

## 📚 File Structure

```
Pages/
├── Patient/
│   ├── Dashboard.cshtml
│   └── Dashboard.cshtml.cs
├── Search/
│   ├── Results.cshtml
│   └── Results.cshtml.cs
├── Appointments/
│   ├── Slots.cshtml
│   └── Slots.cshtml.cs
└── Procedures/
    ├── Request.cshtml
    └── Request.cshtml.cs
```

---

## ✅ Next Pages to Create

1. **Appointment History Page** - Show all appointments with details
2. **Prescription History Page** - List all prescriptions
3. **Lab Test History Page** - Show all lab tests and results
4. **Invoice History Page** - Show all invoices with payment history
5. **Procedure Status Page** - Show all procedure requests
6. **Doctor Profile Page** - Show doctor details and availability
7. **Profile Settings Page** - Edit patient profile
8. **Messages Page** - Patient-doctor messaging

---

## 🎯 Summary

You now have:
✅ 4 fully functional Razor Pages
✅ Complete service integration
✅ Authorization implemented
✅ Form validation
✅ Responsive design
✅ Error handling
✅ Zero compilation errors

**Next:** Create remaining pages and API controllers!

---

**Ready to add more pages? Let's go!** 🚀
