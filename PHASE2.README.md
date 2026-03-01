# Phase 2 Development Requirements - MMGC Hospital Management System

## Overview
This document outlines all the features and requirements that need to be developed in Phase 2 of the MMGC Hospital Management System.

---

## ✅ Phase 1 Completed Features

### Doctor Side - Partially Implemented
- ✅ Doctor Dashboard (basic structure)
- ✅ Profile Management (view/edit)
- ✅ Appointments History (sorted by date)
- ✅ Patients List (with visit & test history)
- ✅ Complete Patient History view

### Database & Infrastructure
- ✅ Added `UserId` field to Doctor model
- ✅ Added `TreatmentSummary` field to Procedure model
- ✅ Added approval fields to LabTest model
- ✅ Created `NursingNote` model
- ✅ Created `PatientVital` model
- ✅ Database migrations applied
- ✅ Role-based authentication and redirection

---

## 🔄 Phase 2 Requirements - To Be Developed

### 1. Doctor Side - Enhancements

#### 1.1 Medical Procedures Module
**Status:** Partially Implemented

**Requirements:**
- [ ] **Procedure Types Enhancement**
  - [ ] Normal Delivery procedures
  - [ ] C-sections procedures
  - [ ] Gynaecological procedures
  - [ ] Ultrasounds procedures
  - [ ] OPD (Outpatient Department) Treatments
  - [ ] IPD (Inpatient Department) Treatments
  - [ ] Procedure type-specific forms and fields

- [ ] **Prescribe Medicines & Tests**
  - [ ] Create prescription from procedure
  - [ ] Link lab tests to procedures
  - [ ] Prescription management interface
  - [ ] Medicine dosage and instructions
  - [ ] Test ordering interface

- [ ] **Treatment Summary**
  - [ ] Generate treatment summary document
  - [ ] Print treatment summary (PDF/HTML)
  - [ ] Save treatment summary to patient record
  - [ ] Email treatment summary to patient
  - [ ] Treatment summary template customization

#### 1.2 Patient Records Module
**Status:** Partially Implemented

**Requirements:**
- [ ] **Complete History View**
  - [x] View appointments history
  - [x] View procedures history
  - [x] View prescriptions history
  - [x] View lab tests history
  - [x] View transactions history
  - [ ] Timeline view of all patient interactions
  - [ ] Filter and search patient history
  - [ ] Export patient history (PDF/Excel)

- [ ] **Report Management**
  - [ ] Upload lab reports (PDF/Images)
  - [ ] Upload ultrasound reports
  - [ ] Upload pathology reports
  - [ ] Approve/reject lab reports
  - [ ] Tag reports with doctor and patient
  - [ ] Report approval workflow
  - [ ] Report versioning
  - [ ] Report comments and notes

#### 1.3 Dashboard Enhancements
**Status:** Basic Implementation Done

**Requirements:**
- [ ] **Statistics & Analytics**
  - [ ] Monthly appointment trends
  - [ ] Revenue charts and graphs
  - [ ] Patient demographics
  - [ ] Procedure type distribution
  - [ ] Performance metrics

- [ ] **Quick Actions**
  - [ ] Quick prescription creation
  - [ ] Quick test ordering
  - [ ] Quick appointment scheduling
  - [ ] Quick patient search

---

### 2. Support Staff - Nurses

**Status:** Controller Created, Views Needed

#### 2.1 Procedure Assistance
**Requirements:**
- [ ] **Assist in Procedures**
  - [ ] View assigned procedures
  - [ ] Procedure assistance checklist
  - [ ] Equipment and supplies tracking
  - [ ] Procedure notes and observations
  - [ ] Real-time procedure updates

#### 2.2 Vitals & Nursing Notes
**Requirements:**
- [ ] **Record Vitals**
  - [ ] Blood Pressure (Systolic/Diastolic)
  - [ ] Temperature
  - [ ] Pulse/Heart Rate
  - [ ] Respiratory Rate
  - [ ] Oxygen Saturation
  - [ ] Weight and Height
  - [ ] Vital signs history chart
  - [ ] Alert system for abnormal vitals

- [ ] **Nursing Notes**
  - [ ] Create nursing notes
  - [ ] Patient progress notes
  - [ ] Medications administered log
  - [ ] Shift handover notes
  - [ ] Notes search and filter
  - [ ] Notes templates

#### 2.3 Patient Progress Updates
**Requirements:**
- [ ] **Update Patient Progress**
  - [ ] Daily progress reports
  - [ ] Condition updates
  - [ ] Medication compliance tracking
  - [ ] Patient response to treatment
  - [ ] Progress notes timeline

#### 2.4 Views to Create
- [ ] `Views/Nurses/Dashboard.cshtml` - Nurses dashboard
- [ ] `Views/Nurses/NursingNotes.cshtml` - List of nursing notes
- [ ] `Views/Nurses/CreateNursingNote.cshtml` - Create nursing note form
- [ ] `Views/Nurses/RecordVitals.cshtml` - Record patient vitals form
- [ ] `Views/Nurses/PatientVitals.cshtml` - Vitals history view
- [ ] `Views/Nurses/UpdatePatientProgress.cshtml` - Update progress form

---

### 3. Support Staff - Reception

**Status:** Not Started

#### 3.1 Patient Registration
**Requirements:**
- [ ] **Manage Patient Registrations**
  - [ ] New patient registration form
  - [ ] Patient information validation
  - [ ] Duplicate patient detection
  - [ ] Patient photo upload
  - [ ] Emergency contact information
  - [ ] Insurance information (if applicable)
  - [ ] Registration history

#### 3.2 MR Number Management
**Requirements:**
- [ ] **MR Number Issuance**
  - [ ] Automatic MR number generation
  - [ ] MR number format configuration
  - [ ] MR number search
  - [ ] MR number validation
  - [ ] MR number re-issuance (if lost)
  - [ ] MR card printing

#### 3.3 Appointment Management
**Requirements:**
- [ ] **Appointment Scheduling**
  - [ ] Schedule new appointments
  - [ ] View doctor availability
  - [ ] Appointment calendar view
  - [ ] Appointment conflict detection
  - [ ] Appointment reminders (SMS/Email)

- [ ] **Appointment Rescheduling**
  - [ ] Reschedule existing appointments
  - [ ] Appointment cancellation
  - [ ] Appointment history
  - [ ] No-show tracking
  - [ ] Waitlist management

#### 3.4 Reception Dashboard
**Requirements:**
- [ ] Today's appointments list
- [ ] Pending registrations
- [ ] Walk-in patients queue
- [ ] Appointment statistics
- [ ] Quick patient search

#### 3.5 Controller & Views to Create
- [ ] `Controllers/ReceptionController.cs`
- [ ] `Services/IReceptionService.cs`
- [ ] `Services/ReceptionService.cs`
- [ ] `Views/Reception/Dashboard.cshtml`
- [ ] `Views/Reception/RegisterPatient.cshtml`
- [ ] `Views/Reception/Appointments.cshtml`
- [ ] `Views/Reception/ScheduleAppointment.cshtml`
- [ ] `Views/Reception/RescheduleAppointment.cshtml`

---

### 4. Support Staff - Lab Staff

**Status:** Partially Implemented (LabTests controller exists)

#### 4.1 Test Booking & Management
**Requirements:**
- [ ] **Book & Manage Test Samples**
  - [ ] Create test booking
  - [ ] Sample collection tracking
  - [ ] Test status updates
  - [ ] Test priority management
  - [ ] Batch test processing
  - [ ] Test queue management
  - [ ] Sample barcode generation

#### 4.2 Report Upload & Management
**Requirements:**
- [ ] **Upload Reports**
  - [ ] Upload lab reports (PDF/Images)
  - [ ] Upload ultrasound reports
  - [ ] Upload pathology reports
  - [ ] Multiple file upload support
  - [ ] Report file validation
  - [ ] Report preview
  - [ ] Report versioning

- [ ] **Tag Reports**
  - [ ] Tag reports with doctor
  - [ ] Tag reports with patient
  - [ ] Tag reports with procedure
  - [ ] Tag reports with appointment
  - [ ] Report metadata management

#### 4.3 Lab Dashboard
**Requirements:**
- [ ] Pending tests queue
- [ ] Tests in progress
- [ ] Completed tests
- [ ] Test statistics
- [ ] Sample collection schedule

#### 4.4 Enhancements Needed
- [ ] Enhance `Controllers/LabTestsController.cs` with:
  - [ ] Bulk test booking
  - [ ] Report upload with tagging
  - [ ] Test status workflow
  - [ ] Lab dashboard action

- [ ] Create `Views/LabTests/LabDashboard.cshtml`
- [ ] Enhance `Views/LabTests/UploadReport.cshtml` with tagging
- [ ] Create `Views/LabTests/BookTests.cshtml`

---

### 5. Support Staff - Accounts Staff

**Status:** Not Started

#### 5.1 Invoice Management
**Requirements:**
- [ ] **Manage Invoices**
  - [ ] Generate invoices
  - [ ] Invoice templates
  - [ ] Invoice numbering system
  - [ ] Invoice items breakdown
  - [ ] Tax calculations
  - [ ] Invoice PDF generation
  - [ ] Invoice email sending
  - [ ] Invoice history

#### 5.2 Payment Processing
**Requirements:**
- [ ] **Payment Management**
  - [ ] Record payments
  - [ ] Multiple payment modes (Cash, Card, UPI, Bank Transfer)
  - [ ] Payment receipts
  - [ ] Partial payment support
  - [ ] Payment reminders
  - [ ] Payment history
  - [ ] Payment reconciliation

#### 5.3 Refunds & Adjustments
**Requirements:**
- [ ] **Refunds**
  - [ ] Process refunds
  - [ ] Refund approval workflow
  - [ ] Refund reasons tracking
  - [ ] Refund history

- [ ] **Adjustments**
  - [ ] Fee adjustments
  - [ ] Discount management
  - [ ] Adjustment approval workflow
  - [ ] Adjustment history

#### 5.4 Financial Reports
**Requirements:**
- [ ] **Generate Reports**
  - [ ] Daily revenue report
  - [ ] Monthly revenue report
  - [ ] Yearly revenue report
  - [ ] Payment mode summary
  - [ ] Outstanding payments report
  - [ ] Refund report
  - [ ] Doctor-wise revenue report
  - [ ] Procedure-wise revenue report
  - [ ] Export reports (PDF/Excel)

#### 5.5 Accounts Dashboard
**Requirements:**
- [ ] Today's revenue
- [ ] Pending payments
- [ ] Outstanding invoices
- [ ] Payment statistics
- [ ] Quick payment entry

#### 5.6 Controller & Views to Create
- [ ] `Controllers/AccountsController.cs`
- [ ] `Services/IAccountsService.cs`
- [ ] `Services/AccountsService.cs`
- [ ] `Models/Invoice.cs` (if not exists)
- [ ] `Models/Payment.cs` (if not exists)
- [ ] `Models/Refund.cs` (if not exists)
- [ ] `Views/Accounts/Dashboard.cshtml`
- [ ] `Views/Accounts/Invoices.cshtml`
- [ ] `Views/Accounts/CreateInvoice.cshtml`
- [ ] `Views/Accounts/Payments.cshtml`
- [ ] `Views/Accounts/RecordPayment.cshtml`
- [ ] `Views/Accounts/Refunds.cshtml`
- [ ] `Views/Accounts/ProcessRefund.cshtml`
- [ ] `Views/Accounts/FinancialReports.cshtml`

---

### 6. General Enhancements

#### 6.1 Navigation & UI
**Requirements:**
- [ ] Update `_LayoutWithSidebar.cshtml` with:
  - [ ] Doctor Dashboard menu (for Doctor role)
  - [ ] Nurses Dashboard menu (for Nurse role)
  - [ ] Reception Dashboard menu (for ReceptionStaff role)
  - [ ] Accounts Dashboard menu (for AccountsStaff role)
  - [ ] Lab Dashboard menu (for LabStaff role)
  - [ ] Role-based menu visibility

#### 6.2 Print & Export Functionality
**Requirements:**
- [ ] Treatment summary printing
- [ ] Prescription printing
- [ ] Invoice printing
- [ ] Patient history export (PDF/Excel)
- [ ] Report printing
- [ ] MR card printing

#### 6.3 Notifications & Alerts
**Requirements:**
- [ ] Email notifications
  - [ ] Appointment reminders
  - [ ] Test report ready
  - [ ] Payment receipts
  - [ ] Treatment summaries

- [ ] SMS notifications (already implemented, enhance)
  - [ ] Appointment confirmations
  - [ ] Test results
  - [ ] Payment reminders

#### 6.4 Search & Filter
**Requirements:**
- [ ] Advanced patient search
- [ ] Advanced appointment search
- [ ] Advanced procedure search
- [ ] Advanced test search
- [ ] Global search functionality

#### 6.5 Reporting & Analytics
**Requirements:**
- [ ] Dashboard analytics
- [ ] Custom report builder
- [ ] Scheduled reports
- [ ] Data export capabilities
- [ ] Charts and graphs

---

## 📋 Development Priority

### High Priority (Core Functionality)
1. ✅ Doctor Dashboard - Basic structure
2. ⚠️ Procedure enhancements (prescriptions, treatment summary)
3. ⚠️ Lab report approval workflow
4. ⚠️ Nurses views (all 6 views)
5. ⚠️ Reception controller and views
6. ⚠️ Accounts controller and views

### Medium Priority (Enhanced Features)
1. Print/Export functionality
2. Email notifications
3. Advanced search
4. Financial reports
5. Analytics and charts

### Low Priority (Nice to Have)
1. Custom report builder
2. Scheduled reports
3. Advanced analytics
4. Mobile responsive enhancements

---

## 🔧 Technical Requirements

### Database
- [x] Migrations for new models
- [ ] Indexes for performance optimization
- [ ] Database backup strategy

### Services
- [ ] Create missing service interfaces
- [ ] Implement service layer for all modules
- [ ] Error handling and logging
- [ ] Unit tests for services

### Controllers
- [ ] Complete all controller actions
- [ ] Authorization checks
- [ ] Input validation
- [ ] Error handling

### Views
- [ ] Create all missing views
- [ ] Responsive design
- [ ] Accessibility compliance
- [ ] Consistent UI/UX

### Security
- [ ] Role-based access control (RBAC) implementation
- [ ] Input sanitization
- [ ] SQL injection prevention
- [ ] XSS prevention
- [ ] CSRF protection

---

## 📝 Notes

1. **Doctor-User Linking**: Currently, doctors need to be manually linked to user accounts via the `UserId` field. Consider implementing an automated linking system or admin interface for this.

2. **File Uploads**: Implement secure file upload handling for:
   - Lab reports
   - Ultrasound reports
   - Patient photos
   - Documents

3. **Print Functionality**: Consider using libraries like:
   - Rotativa (PDF generation)
   - DinkToPdf
   - jsPDF (client-side)

4. **Email Service**: Enhance or implement email service for:
   - Appointment reminders
   - Test results
   - Treatment summaries
   - Invoices

5. **Testing**: Implement unit tests and integration tests for:
   - Services
   - Controllers
   - Critical business logic

---

## 🚀 Getting Started with Phase 2

1. **Review Requirements**: Go through this document and prioritize features
2. **Database Setup**: Ensure all migrations are applied
3. **Service Layer**: Create service interfaces and implementations
4. **Controllers**: Implement controllers with proper authorization
5. **Views**: Create views with consistent UI/UX
6. **Testing**: Test each feature thoroughly
7. **Documentation**: Update documentation as features are completed

---

## 📞 Support

For questions or clarifications about Phase 2 requirements, please refer to:
- `IMPLEMENTATION_STATUS.md` - Current implementation status
- `README.md` - General project documentation
- `README.DOCKER.md` - Docker setup documentation

---

**Last Updated:** January 2025
**Version:** 1.0

