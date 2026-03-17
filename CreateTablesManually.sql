-- OPTIONAL: Manual script for Testimonials and ContactMessages (and other columns).
-- You do NOT need to run this when using the project from Visual Studio.
-- Migrations run automatically on startup (Program.cs calls MigrateAsync()), so
-- Testimonials and ContactMessages tables are created by migration
-- 20260301195000_AddTestimonialsAndContactMessages.
-- Use this script only for one-off fixes or if you cannot run the app (e.g. SSMS only).
-- Run this in SQL Server Management Studio if needed.

USE db_mmgc;

-- Check if Testimonials table exists
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Testimonials')
BEGIN
    CREATE TABLE [Testimonials] (
        [Id] int NOT NULL IDENTITY,
        [PatientId] int NOT NULL,
        [DoctorId] int NOT NULL,
        [Rating] int NOT NULL,
        [Message] nvarchar(1000) NOT NULL,
        [IsApproved] bit NOT NULL DEFAULT 0,
        [CreatedDate] datetime2 NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_Testimonials] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Testimonials_Doctors_DoctorId] FOREIGN KEY ([DoctorId]) REFERENCES [Doctors] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_Testimonials_Patients_PatientId] FOREIGN KEY ([PatientId]) REFERENCES [Patients] ([Id]) ON DELETE CASCADE
    );
    
    CREATE INDEX [IX_Testimonials_DoctorId] ON [Testimonials] ([DoctorId]);
    CREATE INDEX [IX_Testimonials_PatientId] ON [Testimonials] ([PatientId]);
    
    PRINT 'Testimonials table created successfully';
END
ELSE
BEGIN
    PRINT 'Testimonials table already exists';
END;

-- Check if ContactMessages table exists
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'ContactMessages')
BEGIN
    CREATE TABLE [ContactMessages] (
        [Id] int NOT NULL IDENTITY,
        [FirstName] nvarchar(100) NOT NULL,
        [LastName] nvarchar(100) NOT NULL,
        [Email] nvarchar(100) NOT NULL,
        [PhoneNumber] nvarchar(15) NOT NULL,
        [Subject] nvarchar(200) NOT NULL,
        [Message] nvarchar(2000) NOT NULL,
        [Status] nvarchar(20) NOT NULL DEFAULT 'New',
        [AdminNotes] nvarchar(1000) NULL,
        [CreatedDate] datetime2 NOT NULL DEFAULT GETUTCDATE(),
        [ResolvedDate] datetime2 NULL,
        [ResolvedBy] nvarchar(450) NULL,
        CONSTRAINT [PK_ContactMessages] PRIMARY KEY ([Id])
    );
    
    CREATE INDEX [IX_ContactMessages_Email] ON [ContactMessages] ([Email]);
    
    PRINT 'ContactMessages table created successfully';
END
ELSE
BEGIN
    PRINT 'ContactMessages table already exists';
END;

-- Add other columns if they don't exist
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Transactions' AND COLUMN_NAME = 'RowVersion')
BEGIN
    ALTER TABLE [Transactions] ADD [RowVersion] rowversion NULL;
    PRINT 'RowVersion column added to Transactions';
END;

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Procedures' AND COLUMN_NAME = 'Findings')
BEGIN
    ALTER TABLE [Procedures] ADD [Findings] nvarchar(max) NULL;
    PRINT 'Findings column added to Procedures';
END;

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Prescriptions' AND COLUMN_NAME = 'ValidUntil')
BEGIN
    ALTER TABLE [Prescriptions] ADD [ValidUntil] datetime2 NULL;
    PRINT 'ValidUntil column added to Prescriptions';
END;

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'LabTests' AND COLUMN_NAME = 'RowVersion')
BEGIN
    ALTER TABLE [LabTests] ADD [RowVersion] rowversion NULL;
    PRINT 'RowVersion column added to LabTests';
END;

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'DoctorSchedules' AND COLUMN_NAME = 'ScheduleDate')
BEGIN
    ALTER TABLE [DoctorSchedules] ADD [ScheduleDate] datetime2 NOT NULL DEFAULT '2025-01-01';
    PRINT 'ScheduleDate column added to DoctorSchedules';
END;

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Patients' AND COLUMN_NAME = 'UserId')
BEGIN
    ALTER TABLE [Patients] ADD [UserId] nvarchar(450) NULL;
    PRINT 'UserId column added to Patients';
END;

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Appointments' AND COLUMN_NAME = 'RowVersion')
BEGIN
    ALTER TABLE [Appointments] ADD [RowVersion] rowversion NULL;
    PRINT 'RowVersion column added to Appointments';
END;

-- Record the migration in the history
IF NOT EXISTS (SELECT 1 FROM __EFMigrationsHistory WHERE MigrationId = '20260301195000_AddTestimonialsAndContactMessages')
BEGIN
    INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion) 
    VALUES ('20260301195000_AddTestimonialsAndContactMessages', '8.0.0');
    PRINT 'Migration recorded in history';
END;

-- Verify tables exist
SELECT 
    CASE 
        WHEN EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Testimonials') THEN 'Testimonials: OK'
        ELSE 'Testimonials: MISSING'
    END as Testimonials,
    CASE 
        WHEN EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'ContactMessages') THEN 'ContactMessages: OK'
        ELSE 'ContactMessages: MISSING'
    END as ContactMessages;
