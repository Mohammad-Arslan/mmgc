using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MMGC.Models;

namespace MMGC.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // DbSets
    public DbSet<Patient> Patients { get; set; }
    public DbSet<Doctor> Doctors { get; set; }
    public DbSet<Nurse> Nurses { get; set; }
    public DbSet<Appointment> Appointments { get; set; }
    public DbSet<Procedure> Procedures { get; set; }
    public DbSet<ProcedureRequest> ProcedureRequests { get; set; }
    public DbSet<LabTestCategory> LabTestCategories { get; set; }
    public DbSet<LabTest> LabTests { get; set; }
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<Prescription> Prescriptions { get; set; }
    public DbSet<DoctorSchedule> DoctorSchedules { get; set; }
    public DbSet<NursingNote> NursingNotes { get; set; }
    public DbSet<PatientVital> PatientVitals { get; set; }
    public DbSet<ReceptionStaff> ReceptionStaffs { get; set; }
    public DbSet<AccountsStaff> AccountsStaffs { get; set; }
    public DbSet<NotificationLog> NotificationLogs { get; set; }
    public DbSet<DocumentAuditLog> DocumentAuditLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Configure relationships and constraints
        builder.Entity<Appointment>()
            .HasOne(a => a.Patient)
            .WithMany(p => p.Appointments)
            .HasForeignKey(a => a.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Appointment>()
            .HasOne(a => a.Doctor)
            .WithMany(d => d.Appointments)
            .HasForeignKey(a => a.DoctorId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<Appointment>()
            .HasOne(a => a.Nurse)
            .WithMany(n => n.Appointments)
            .HasForeignKey(a => a.NurseId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<Procedure>()
            .HasOne(p => p.Patient)
            .WithMany(p => p.Procedures)
            .HasForeignKey(p => p.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Procedure>()
            .HasOne(p => p.Doctor)
            .WithMany(d => d.Procedures)
            .HasForeignKey(p => p.DoctorId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Procedure>()
            .HasOne(p => p.Nurse)
            .WithMany(n => n.Procedures)
            .HasForeignKey(p => p.NurseId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<LabTest>()
            .HasOne(lt => lt.Patient)
            .WithMany(p => p.LabTests)
            .HasForeignKey(lt => lt.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<LabTest>()
            .HasOne(lt => lt.LabTestCategory)
            .WithMany(c => c.LabTests)
            .HasForeignKey(lt => lt.LabTestCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<LabTest>()
            .HasOne(lt => lt.Procedure)
            .WithMany(p => p.LabTests)
            .HasForeignKey(lt => lt.ProcedureId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<LabTest>()
            .HasOne(lt => lt.ApprovedByDoctor)
            .WithMany()
            .HasForeignKey(lt => lt.ApprovedByDoctorId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<Transaction>()
            .HasOne(t => t.Patient)
            .WithMany(p => p.Transactions)
            .HasForeignKey(t => t.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Transaction>()
            .HasOne(t => t.Procedure)
            .WithMany()
            .HasForeignKey(t => t.ProcedureId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<Prescription>()
            .HasOne(pr => pr.Patient)
            .WithMany(p => p.Prescriptions)
            .HasForeignKey(pr => pr.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Prescription>()
            .HasOne(pr => pr.Doctor)
            .WithMany()
            .HasForeignKey(pr => pr.DoctorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Prescription>()
            .HasOne(pr => pr.Procedure)
            .WithMany()
            .HasForeignKey(pr => pr.ProcedureId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<DoctorSchedule>()
            .HasOne(ds => ds.Doctor)
            .WithMany(d => d.Schedules)
            .HasForeignKey(ds => ds.DoctorId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes for performance
        builder.Entity<Patient>()
            .HasIndex(p => p.MRNumber)
            .IsUnique();

        builder.Entity<Appointment>()
            .HasIndex(a => a.AppointmentDate);

        builder.Entity<Transaction>()
            .HasIndex(t => t.TransactionDate);

        // Configure NursingNote relationships
        builder.Entity<NursingNote>()
            .HasOne(nn => nn.Patient)
            .WithMany()
            .HasForeignKey(nn => nn.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<NursingNote>()
            .HasOne(nn => nn.Procedure)
            .WithMany()
            .HasForeignKey(nn => nn.ProcedureId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<NursingNote>()
            .HasOne(nn => nn.Appointment)
            .WithMany()
            .HasForeignKey(nn => nn.AppointmentId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<NursingNote>()
            .HasOne(nn => nn.Nurse)
            .WithMany()
            .HasForeignKey(nn => nn.NurseId)
            .OnDelete(DeleteBehavior.Restrict);

        // Configure PatientVital relationships
        builder.Entity<PatientVital>()
            .HasOne(pv => pv.Patient)
            .WithMany()
            .HasForeignKey(pv => pv.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<PatientVital>()
            .HasOne(pv => pv.Procedure)
            .WithMany()
            .HasForeignKey(pv => pv.ProcedureId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<PatientVital>()
            .HasOne(pv => pv.Appointment)
            .WithMany()
            .HasForeignKey(pv => pv.AppointmentId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<PatientVital>()
            .HasOne(pv => pv.Nurse)
            .WithMany()
            .HasForeignKey(pv => pv.NurseId)
            .OnDelete(DeleteBehavior.SetNull);

        // Configure ProcedureRequest relationships
        builder.Entity<ProcedureRequest>()
            .HasOne(pr => pr.Patient)
            .WithMany()
            .HasForeignKey(pr => pr.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ProcedureRequest>()
            .HasOne(pr => pr.AssignedDoctor)
            .WithMany()
            .HasForeignKey(pr => pr.DoctorId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<ProcedureRequest>()
            .HasOne(pr => pr.LinkedProcedure)
            .WithMany()
            .HasForeignKey(pr => pr.LinkedProcedureId)
            .OnDelete(DeleteBehavior.SetNull);

        // Configure NotificationLog relationships
        builder.Entity<NotificationLog>()
            .HasOne(nl => nl.Patient)
            .WithMany()
            .HasForeignKey(nl => nl.PatientId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<NotificationLog>()
            .HasOne(nl => nl.Appointment)
            .WithMany()
            .HasForeignKey(nl => nl.AppointmentId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<NotificationLog>()
            .HasOne(nl => nl.ProcedureRequest)
            .WithMany()
            .HasForeignKey(nl => nl.ProcedureRequestId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<NotificationLog>()
            .HasOne(nl => nl.Transaction)
            .WithMany()
            .HasForeignKey(nl => nl.TransactionId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<NotificationLog>()
            .HasOne(nl => nl.LabTest)
            .WithMany()
            .HasForeignKey(nl => nl.LabTestId)
            .OnDelete(DeleteBehavior.SetNull);

        // Configure DocumentAuditLog relationships
        builder.Entity<DocumentAuditLog>()
            .HasOne(dal => dal.Patient)
            .WithMany()
            .HasForeignKey(dal => dal.PatientId)
            .OnDelete(DeleteBehavior.SetNull);

        // Add indexes for frequently queried columns
        builder.Entity<NotificationLog>()
            .HasIndex(nl => nl.NotificationId)
            .IsUnique();

        builder.Entity<NotificationLog>()
            .HasIndex(nl => nl.CreatedAt);

        builder.Entity<NotificationLog>()
            .HasIndex(nl => nl.Status);

        builder.Entity<DocumentAuditLog>()
            .HasIndex(dal => dal.GeneratedAt);

        builder.Entity<ProcedureRequest>()
            .HasIndex(pr => pr.Status);
    }
}

