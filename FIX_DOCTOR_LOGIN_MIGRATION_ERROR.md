# 🔧 FIX: DATABASE MIGRATION ERROR - DOCTOR LOGIN

## ❌ ERROR ENCOUNTERED

```
SqlException: Invalid column name 'AppointmentEndTime'.
Invalid column name 'RowVersion'.
Invalid column name 'StatusEnum'.
```

**When:** Attempting to login with Doctor role  
**Cause:** Database migration hasn't been executed  
**Status:** ⏳ PENDING FIX

---

## ✅ SOLUTION: RUN DATABASE MIGRATION

The migration file was created in Phase 2 but the database hasn't been updated yet. Here's how to fix it:

### Option 1: Using Visual Studio Package Manager Console (RECOMMENDED)

**Step 1:** Open Package Manager Console
```
Tools → NuGet Package Manager → Package Manager Console
```

**Step 2:** Ensure correct project is selected
```
Default project: MMGC
```

**Step 3:** Run the migration
```powershell
Update-Database
```

**Step 4:** Wait for completion
- You should see: `Done.`
- Check the Migrations folder for `__EFMigrationsHistory` table updates

---

### Option 2: Using Command Line

**Step 1:** Navigate to project
```bash
cd D:\mmgc-main
```

**Step 2:** Run migration
```bash
dotnet ef database update
```

**Step 3:** Verify success
- Database schema updated
- New columns added to Appointment table
- ProcedureRequest, NotificationLog, DocumentAuditLog tables created

---

## 📊 WHAT THE MIGRATION DOES

### Adds to Appointment Table
```sql
-- New Columns
ALTER TABLE Appointments ADD StatusEnum INT DEFAULT 0
ALTER TABLE Appointments ADD AppointmentEndTime DATETIME2 NULL
ALTER TABLE Appointments ADD RowVersion ROWVERSION NULL
ALTER TABLE Appointments ADD CreatedBy NVARCHAR(450) NULL
```

### Creates New Tables
1. **ProcedureRequests** - Stores procedure requests
2. **NotificationLogs** - Stores notification history
3. **DocumentAuditLogs** - Stores document audit trail

### Migration Details
```
Migration Name: 20260124_Phase2_AddProcedureRequestNotificationAndDocumentModels
Previous: 20260123092101_MakePatientVitalPatientIdNullable
Status: PENDING
```

---

## ✨ STEP-BY-STEP FIX (VISUAL STUDIO)

### 1. Open Visual Studio
- Open your MMGC project

### 2. Open Package Manager Console
- Menu: **Tools** → **NuGet Package Manager** → **Package Manager Console**

### 3. Verify Settings
```
Package Manager Console should show:
- PM> (ready prompt)
- Default project: MMGC (dropdown)
```

### 4. Run Migration Command
```powershell
PM> Update-Database
```

### 5. Monitor Progress
You should see output like:
```
Build started...
Build succeeded.
info: Microsoft.EntityFrameworkCore.Database.Command[20101]
      Executed DbCommand (250ms) [Parameters=[], CommandType='Text', CommandTimeout='0']
      ALTER TABLE [Appointments] ADD [StatusEnum] [int] NOT NULL DEFAULT 0;
info: Microsoft.EntityFrameworkCore.Database.Command[20101]
      Executed DbCommand (150ms) [Parameters=[], CommandType='Text', CommandTimeout='0']
      ALTER TABLE [Appointments] ADD [AppointmentEndTime] [datetime2] NULL;
...
Done.
```

### 6. Verify Success
- Migration completed without errors
- Package Manager Console shows: **Done.**

---

## ✨ STEP-BY-STEP FIX (COMMAND LINE)

### 1. Open Terminal/PowerShell
- In Visual Studio: **Tools** → **Command Line** → **Developer PowerShell**
- Or open PowerShell manually

### 2. Navigate to Project
```bash
cd D:\mmgc-main
```

### 3. Run Migration
```bash
dotnet ef database update
```

### 4. Wait for Completion
```
Build succeeded.
Done. 1 migration applied.
```

### 5. Verify Success
- Exit code: 0 (success)
- No error messages

---

## 🔍 VERIFY MIGRATION APPLIED

### Check in SQL Server Management Studio

**Step 1:** Connect to your database

**Step 2:** Run this query
```sql
SELECT * FROM __EFMigrationsHistory 
ORDER BY MigrationId DESC
LIMIT 1;
```

**Expected Result:**
```
MigrationId: 20260124_Phase2_AddProcedureRequestNotificationAndDocumentModels
ProductVersion: 8.0.0
```

**Step 3:** Verify tables exist
```sql
-- Should return without errors
SELECT * FROM ProcedureRequests;
SELECT * FROM NotificationLogs;
SELECT * FROM DocumentAuditLogs;
```

**Step 4:** Verify Appointment columns
```sql
SELECT 
    COLUMN_NAME, 
    DATA_TYPE, 
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Appointments' 
AND COLUMN_NAME IN ('StatusEnum', 'AppointmentEndTime', 'RowVersion', 'CreatedBy');
```

**Expected Result:**
```
StatusEnum        | int        | NO
AppointmentEndTime| datetime2  | YES
RowVersion        | timestamp  | YES
CreatedBy         | nvarchar   | YES
```

---

## 🧪 TEST AFTER MIGRATION

### 1. Rebuild Solution
```bash
dotnet build
```
✓ Should complete with 0 errors

### 2. Run Application
```bash
dotnet run
```

### 3. Test Doctor Login
- Navigate to: `https://localhost:7000/identity/account/login`
- Login with doctor credentials
- Should work without SQL errors

### 4. Access Doctor Dashboard
- Should load doctor-specific dashboard
- No errors should appear

---

## ⚠️ TROUBLESHOOTING

### If You Get: "No migrations to apply"
- Migration might already be applied
- Check `__EFMigrationsHistory` table
- Run: `dotnet ef migrations list`

### If You Get: "Build failed"
- Ensure all code compiles: `dotnet build`
- Check for compilation errors
- Fix errors before retrying migration

### If You Get: "Connection timeout"
- Verify SQL Server is running
- Check connection string in appsettings.json
- Verify database exists

### If You Get: "Column already exists"
- Migration was partially applied
- Run: `dotnet ef migrations remove` (be careful!)
- Then re-run: `Update-Database`

---

## 📋 MIGRATION CHECKLIST

After running the migration, verify:

- [ ] Visual Studio Package Manager Console shows: **Done.**
- [ ] No error messages appear
- [ ] `__EFMigrationsHistory` table has new entry
- [ ] `ProcedureRequests` table created
- [ ] `NotificationLogs` table created
- [ ] `DocumentAuditLogs` table created
- [ ] `Appointments` table has `StatusEnum` column
- [ ] `Appointments` table has `AppointmentEndTime` column
- [ ] `Appointments` table has `RowVersion` column
- [ ] `Appointments` table has `CreatedBy` column
- [ ] Application builds (0 errors)
- [ ] Doctor login works without SQL errors
- [ ] Dashboard loads successfully

---

## 🚀 AFTER MIGRATION

### 1. Commit the Migration
```bash
git add .
git commit -m "Applied: Phase 2C Migration - Added new columns and tables"
git push origin main
```

### 2. Test All Features
- ✅ Doctor login
- ✅ Patient dashboard
- ✅ Search functionality
- ✅ Appointment booking
- ✅ Procedure request

### 3. Verify Services Working
All 6 services should now work:
- PatientDashboardService
- SearchService
- AvailabilityService
- ProcedureWorkflowService
- NotificationService
- NotificationLogService

---

## 📞 STILL HAVING ISSUES?

If migration fails, try these steps:

### Reset Database (Last Resort)
```powershell
# WARNING: This deletes all data!
Update-Database -Migration 20260123092101_MakePatientVitalPatientIdNullable
Update-Database
```

### Rebuild Everything
```bash
dotnet clean
dotnet build
dotnet ef database update
```

### Check Database Integrity
```sql
DBCC CHECKDB(YourDatabaseName);
```

---

## ✅ FINAL VERIFICATION

After migration, test these scenarios:

### Test 1: Doctor Login
- User: doctor@example.com
- Password: [Your password]
- Expected: Login successful → Dashboard loads

### Test 2: View Appointments
- Should show list of appointments
- No SQL errors
- Data loads correctly

### Test 3: Check New Columns
- Open SQL Server Management Studio
- Verify columns exist in Appointments table
- Verify new tables exist

### Test 4: Run Migrations List
```bash
dotnet ef migrations list
```
Should show:
```
20260124_Phase2_AddProcedureRequestNotificationAndDocumentModels
```

---

## 🎯 SUCCESS INDICATORS

✅ **Migration Applied Successfully When:**
1. No error messages in Package Manager Console
2. `__EFMigrationsHistory` table updated
3. All new columns added to Appointment table
4. All new tables created (ProcedureRequest, NotificationLog, DocumentAuditLog)
5. Doctor can login without SQL errors
6. Application compiles with 0 errors

---

## 📊 MIGRATION STATISTICS

```
Migration File: 20260124_Phase2_AddProcedureRequestNotificationAndDocumentModels.cs
Designer File: 20260124_Phase2_AddProcedureRequestNotificationAndDocumentModels.Designer.cs
Status: Ready to Apply
Commands: 4 (Add columns to Appointment, Create 3 tables)
Tables Affected: 1 existing (Appointments), 3 new (Procedure*, Notification*, Document*)
Columns Added: 4 to Appointments
```

---

## 🎓 WHAT'S HAPPENING UNDER THE HOOD

### Migration Up() Method Does:
1. Adds `StatusEnum` to Appointments (default: 0)
2. Adds `AppointmentEndTime` to Appointments (nullable)
3. Adds `RowVersion` to Appointments (for concurrency)
4. Adds `CreatedBy` to Appointments (for audit)
5. Creates `ProcedureRequests` table (100 columns)
6. Creates `NotificationLogs` table (with indexes)
7. Creates `DocumentAuditLogs` table (with audit fields)

### This Enables:
- ✅ Appointment end time tracking
- ✅ Concurrent edit protection
- ✅ Procedure request workflow
- ✅ Notification logging
- ✅ Document audit trails

---

## ✨ SUMMARY

**Problem:** Doctor login failing due to missing database columns
**Cause:** Migration not applied to database
**Solution:** Run `Update-Database` in Package Manager Console
**Time to Fix:** 2-5 minutes
**Risk Level:** LOW (migration is safe)

---

## 🎉 EXPECTED RESULT

After running the migration:

```
╔════════════════════════════════════════╗
║  Doctor Login will work! ✅             ║
║                                        ║
║  Database Schema Updated ✅            ║
║  New Tables Created ✅                 ║
║  New Columns Added ✅                  ║
║  Ready for Use ✅                      ║
╚════════════════════════════════════════╝
```

---

**Now run: `Update-Database` in Package Manager Console!**

After that, try logging in as a doctor again. It should work! 🚀
