# Phase 2 Implementation Plan - Enterprise Architecture

## 🎯 Executive Summary

This document outlines the complete Phase 2 implementation using enterprise architecture patterns:
- Feature-based folder structure
- Service layer abstraction
- SOLID principles
- Async-first design
- Security by default
- Production-grade code

---

## 📐 Folder Structure (New)

```
MMGC/
├── Features/
│   ├── Patients/
│   │   ├── Pages/
│   │   │   └── Dashboard.cshtml
│   │   ├── ViewModels/
│   │   │   └── PatientDashboardViewModel.cs
│   │   └── Controllers/
│   │       └── PatientDashboardController.cs
│   │
│   ├── Appointments/
│   │   ├── Services/
│   │   │   └── AvailabilityService.cs
│   │   └── Controllers/
│   │
│   ├── Procedures/
│   │   ├── Services/
│   │   │   └── ProcedureWorkflowService.cs
│   │   └── ViewModels/
│   │
│   ├── Reports/
│   │   ├── Services/
│   │   │   └── PdfService.cs
│   │   ├── Controllers/
│   │   │   └── ReportsController.cs
│   │   └── Templates/
│   │
│   ├── Search/
│   │   ├── Services/
│   │   │   └── SearchService.cs
│   │   └── Controllers/
│   │       └── SearchController.cs
│   │
│   └── Notifications/
│       ├── Services/
│       │   ├── NotificationService.cs
│       │   ├── Providers/
│       │   │   ├── SmsNotificationProvider.cs
│       │   │   └── EmailNotificationProvider.cs
│       │   └── INotificationProvider.cs
│       └── Models/
│
├── Shared/
│   ├── Interfaces/
│   │   ├── IPatientDashboardService.cs
│   │   ├── INotificationService.cs
│   │   ├── IPdfService.cs
│   │   ├── ISearchService.cs
│   │   ├── IAvailabilityService.cs
│   │   └── IProcedureWorkflowService.cs
│   │
│   ├── DTOs/
│   │   ├── PatientDashboardDto.cs
│   │   ├── AppointmentSlotDto.cs
│   │   ├── SearchResultDto.cs
│   │   ├── NotificationMessageDto.cs
│   │   └── ProcedureRequestDto.cs
│   │
│   ├── ViewModels/
│   │   └── PatientDashboardViewModel.cs
│   │
│   ├── Enums/
│   │   ├── NotificationTypeEnum.cs
│   │   ├── ProcedureStatusEnum.cs
│   │   └── AppointmentStatusEnum.cs
│   │
│   ├── Constants/
│   │   └── SystemConstants.cs
│   │
│   ├── Infrastructure/
│   │   └── Services/
│   │       ├── PdfGenerationService.cs
│   │       └── NotificationLogService.cs
│   │
│   └── Exceptions/
│       └── SystemExceptions.cs
│
├── Data/
│   ├── Migrations/
│   └── ApplicationDbContext.cs (updated)
│
└── Models/ (existing, updated)
    ├── Appointment.cs (enhanced)
    ├── Procedure.cs (enhanced)
    ├── ProcedureRequest.cs (new)
    ├── DocumentAuditLog.cs (new)
    ├── NotificationLog.cs (new)
    └── ...
```

---

## 🔗 Dependency Injection Flow

```
Program.cs (Updated)
├── RegisterFeatureServices()
│   ├── IPatientDashboardService → PatientDashboardService
│   ├── IPdfService → PdfService
│   ├── INotificationService → NotificationService
│   ├── ISearchService → SearchService
│   ├── IAvailabilityService → AvailabilityService
│   └── IProcedureWorkflowService → ProcedureWorkflowService
│
├── RegisterNotificationProviders()
│   ├── INotificationProvider (Sms) → SmsNotificationProvider
│   └── INotificationProvider (Email) → EmailNotificationProvider
│
└── RegisterInfrastructure()
    ├── IPdfGenerationService → PdfGenerationService
    └── INotificationLogService → NotificationLogService
```

---

## 📋 Implementation Checklist

### Phase 2A: Foundational (This Document)
- [ ] Create all interface contracts
- [ ] Create all DTOs and Enums
- [ ] Update models with new fields/migrations
- [ ] Create DatabaseAuditLog and NotificationLog tables

### Phase 2B: Service Layer
- [ ] PatientDashboardService
- [ ] PdfService
- [ ] NotificationService + Providers
- [ ] SearchService
- [ ] AvailabilityService
- [ ] ProcedureWorkflowService

### Phase 2C: UI Layer
- [ ] PatientDashboardController + Page
- [ ] ReportsController (PDF endpoints)
- [ ] SearchController
- [ ] Enhanced AppointmentsController
- [ ] ProcedureRequestController

### Phase 2D: Integration
- [ ] Wire up all services in Program.cs
- [ ] Configure Twilio/SendGrid
- [ ] Add notification triggers
- [ ] Add authorization policies

### Phase 2E: Polish
- [ ] Error handling middleware
- [ ] Logging and tracing
- [ ] Unit test structure
- [ ] Documentation

---

## 🔐 Security Model

All endpoints require [Authorize] attribute.

```csharp
// Read own data
[Authorize(Roles = "Patient")]
public IActionResult ViewDashboard() { }

// Approve procedures
[Authorize(Roles = "Doctor")]
public async Task<IActionResult> ApproveProcedure(int id) { }

// Generate reports
[Authorize(Roles = "Staff,Admin")]
public async Task<IActionResult> DownloadReport(int id) { }
```

---

## 📊 Data Models Overview

### New Models
- **ProcedureRequest** - Workflow for procedure approval
- **NotificationLog** - Track all sent notifications
- **DocumentAuditLog** - Track PDF generation
- **PdfTemplate** - Store template metadata

### Enhanced Models
- **Appointment** - Add Status enum, RowVersion (concurrency)
- **Procedure** - Enhanced with request tracking
- **Patient** - Link to NotificationPreferences

---

## 🚀 Next Step

All implementation files will be created following this plan.
All files follow enterprise patterns: SOLID, Clean Architecture, DDD principles.

