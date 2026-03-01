using Microsoft.EntityFrameworkCore.Migrations;
using MMGC.Shared.Enums;

#nullable disable

namespace MMGC.Migrations;

public partial class Phase2_AddProcedureRequestNotificationAndDocumentModels : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Add Status enum column and new fields to Appointments
        migrationBuilder.AddColumn<int>(
            name: "StatusEnum",
            table: "Appointments",
            type: "int",
            nullable: false,
            defaultValue: (int)AppointmentStatusEnum.Scheduled);

        migrationBuilder.AddColumn<DateTime>(
            name: "AppointmentEndTime",
            table: "Appointments",
            type: "datetime2",
            nullable: true);

        migrationBuilder.AddColumn<byte[]>(
            name: "RowVersion",
            table: "Appointments",
            type: "rowversion",
            rowVersion: true,
            nullable: true);

        // Note: CreatedBy column already exists in Appointments table, so we don't add it again
        // Removed: migrationBuilder.AddColumn<string> for CreatedBy to avoid duplicate column error

        // Create ProcedureRequest table
        migrationBuilder.CreateTable(
            name: "ProcedureRequests",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                PatientId = table.Column<int>(type: "int", nullable: false),
                DoctorId = table.Column<int>(type: "int", nullable: true),
                ProcedureType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                ReasonForProcedure = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                RequestedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                Status = table.Column<int>(type: "int", nullable: false, defaultValue: (int)ProcedureStatusEnum.Requested),
                ApprovalComments = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                ReviewedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                ScheduledDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                LinkedProcedureId = table.Column<int>(type: "int", nullable: true),
                CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ProcedureRequests", x => x.Id);
                table.ForeignKey(
                    name: "FK_ProcedureRequests_Doctors_DoctorId",
                    column: x => x.DoctorId,
                    principalTable: "Doctors",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
                table.ForeignKey(
                    name: "FK_ProcedureRequests_Patients_PatientId",
                    column: x => x.PatientId,
                    principalTable: "Patients",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_ProcedureRequests_Procedures_LinkedProcedureId",
                    column: x => x.LinkedProcedureId,
                    principalTable: "Procedures",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.NoAction);  // Changed from SetNull to NoAction to avoid cascade conflict
            });

        // Create NotificationLog table
        migrationBuilder.CreateTable(
            name: "NotificationLogs",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                NotificationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                RecipientContact = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                NotificationType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                NotificationTypeEnum = table.Column<int>(type: "int", nullable: false),
                MessageContent = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "Pending"),
                ErrorMessage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                RetryCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                ExternalMessageId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                PatientId = table.Column<int>(type: "int", nullable: true),
                AppointmentId = table.Column<int>(type: "int", nullable: true),
                ProcedureRequestId = table.Column<int>(type: "int", nullable: true),
                TransactionId = table.Column<int>(type: "int", nullable: true),
                LabTestId = table.Column<int>(type: "int", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                DeliveredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                LastRetryAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                TriggeredBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_NotificationLogs", x => x.Id);
                table.ForeignKey(
                    name: "FK_NotificationLogs_Appointments_AppointmentId",
                    column: x => x.AppointmentId,
                    principalTable: "Appointments",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
                table.ForeignKey(
                    name: "FK_NotificationLogs_LabTests_LabTestId",
                    column: x => x.LabTestId,
                    principalTable: "LabTests",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
                table.ForeignKey(
                    name: "FK_NotificationLogs_Patients_PatientId",
                    column: x => x.PatientId,
                    principalTable: "Patients",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
                table.ForeignKey(
                    name: "FK_NotificationLogs_ProcedureRequests_ProcedureRequestId",
                    column: x => x.ProcedureRequestId,
                    principalTable: "ProcedureRequests",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
                table.ForeignKey(
                    name: "FK_NotificationLogs_Transactions_TransactionId",
                    column: x => x.TransactionId,
                    principalTable: "Transactions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
            });

        // Create DocumentAuditLog table
        migrationBuilder.CreateTable(
            name: "DocumentAuditLogs",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                DocumentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                EntityId = table.Column<int>(type: "int", nullable: false),
                PatientId = table.Column<int>(type: "int", nullable: true),
                RequestedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                FileHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                GeneratedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                RequestorIpAddress = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                WasDownloaded = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                DownloadedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "Success"),
                ErrorMessage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DocumentAuditLogs", x => x.Id);
                table.ForeignKey(
                    name: "FK_DocumentAuditLogs_Patients_PatientId",
                    column: x => x.PatientId,
                    principalTable: "Patients",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
            });

        // Create indexes
        migrationBuilder.CreateIndex(
            name: "IX_ProcedureRequests_Status",
            table: "ProcedureRequests",
            column: "Status");

        migrationBuilder.CreateIndex(
            name: "IX_ProcedureRequests_PatientId",
            table: "ProcedureRequests",
            column: "PatientId");

        migrationBuilder.CreateIndex(
            name: "IX_ProcedureRequests_DoctorId",
            table: "ProcedureRequests",
            column: "DoctorId");

        migrationBuilder.CreateIndex(
            name: "IX_NotificationLogs_NotificationId",
            table: "NotificationLogs",
            column: "NotificationId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_NotificationLogs_CreatedAt",
            table: "NotificationLogs",
            column: "CreatedAt");

        migrationBuilder.CreateIndex(
            name: "IX_NotificationLogs_Status",
            table: "NotificationLogs",
            column: "Status");

        migrationBuilder.CreateIndex(
            name: "IX_NotificationLogs_PatientId",
            table: "NotificationLogs",
            column: "PatientId");

        migrationBuilder.CreateIndex(
            name: "IX_DocumentAuditLogs_GeneratedAt",
            table: "DocumentAuditLogs",
            column: "GeneratedAt");

        migrationBuilder.CreateIndex(
            name: "IX_DocumentAuditLogs_PatientId",
            table: "DocumentAuditLogs",
            column: "PatientId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "DocumentAuditLogs");

        migrationBuilder.DropTable(
            name: "NotificationLogs");

        migrationBuilder.DropTable(
            name: "ProcedureRequests");

        migrationBuilder.DropColumn(
            name: "StatusEnum",
            table: "Appointments");

        migrationBuilder.DropColumn(
            name: "AppointmentEndTime",
            table: "Appointments");

        migrationBuilder.DropColumn(
            name: "RowVersion",
            table: "Appointments");
    }
}
