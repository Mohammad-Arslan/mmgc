using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MMGC.Migrations
{
    /// <inheritdoc />
    public partial class AddDoctorDashboardAndSupportStaffFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TreatmentSummary",
                table: "Procedures",
                type: "nvarchar(max)",
                maxLength: 5000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ApprovedByDoctorId",
                table: "LabTests",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedDate",
                table: "LabTests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsApproved",
                table: "LabTests",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "Doctors",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "PatientId",
                table: "Appointments",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.CreateTable(
                name: "NursingNotes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatientId = table.Column<int>(type: "int", nullable: false),
                    ProcedureId = table.Column<int>(type: "int", nullable: true),
                    AppointmentId = table.Column<int>(type: "int", nullable: true),
                    NurseId = table.Column<int>(type: "int", nullable: false),
                    NoteDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Vitals = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PatientProgress = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    MedicationsAdministered = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NursingNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NursingNotes_Appointments_AppointmentId",
                        column: x => x.AppointmentId,
                        principalTable: "Appointments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_NursingNotes_Nurses_NurseId",
                        column: x => x.NurseId,
                        principalTable: "Nurses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NursingNotes_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NursingNotes_Procedures_ProcedureId",
                        column: x => x.ProcedureId,
                        principalTable: "Procedures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "PatientVitals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatientId = table.Column<int>(type: "int", nullable: false),
                    ProcedureId = table.Column<int>(type: "int", nullable: true),
                    AppointmentId = table.Column<int>(type: "int", nullable: true),
                    NurseId = table.Column<int>(type: "int", nullable: true),
                    RecordedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BloodPressureSystolic = table.Column<int>(type: "int", nullable: true),
                    BloodPressureDiastolic = table.Column<int>(type: "int", nullable: true),
                    Temperature = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    Pulse = table.Column<int>(type: "int", nullable: true),
                    RespiratoryRate = table.Column<int>(type: "int", nullable: true),
                    OxygenSaturation = table.Column<int>(type: "int", nullable: true),
                    Weight = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    Height = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RecordedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientVitals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PatientVitals_Appointments_AppointmentId",
                        column: x => x.AppointmentId,
                        principalTable: "Appointments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PatientVitals_Nurses_NurseId",
                        column: x => x.NurseId,
                        principalTable: "Nurses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PatientVitals_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PatientVitals_Procedures_ProcedureId",
                        column: x => x.ProcedureId,
                        principalTable: "Procedures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LabTests_ApprovedByDoctorId",
                table: "LabTests",
                column: "ApprovedByDoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_NursingNotes_AppointmentId",
                table: "NursingNotes",
                column: "AppointmentId");

            migrationBuilder.CreateIndex(
                name: "IX_NursingNotes_NurseId",
                table: "NursingNotes",
                column: "NurseId");

            migrationBuilder.CreateIndex(
                name: "IX_NursingNotes_PatientId",
                table: "NursingNotes",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_NursingNotes_ProcedureId",
                table: "NursingNotes",
                column: "ProcedureId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientVitals_AppointmentId",
                table: "PatientVitals",
                column: "AppointmentId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientVitals_NurseId",
                table: "PatientVitals",
                column: "NurseId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientVitals_PatientId",
                table: "PatientVitals",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientVitals_ProcedureId",
                table: "PatientVitals",
                column: "ProcedureId");

            migrationBuilder.AddForeignKey(
                name: "FK_LabTests_Doctors_ApprovedByDoctorId",
                table: "LabTests",
                column: "ApprovedByDoctorId",
                principalTable: "Doctors",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LabTests_Doctors_ApprovedByDoctorId",
                table: "LabTests");

            migrationBuilder.DropTable(
                name: "NursingNotes");

            migrationBuilder.DropTable(
                name: "PatientVitals");

            migrationBuilder.DropIndex(
                name: "IX_LabTests_ApprovedByDoctorId",
                table: "LabTests");

            migrationBuilder.DropColumn(
                name: "TreatmentSummary",
                table: "Procedures");

            migrationBuilder.DropColumn(
                name: "ApprovedByDoctorId",
                table: "LabTests");

            migrationBuilder.DropColumn(
                name: "ApprovedDate",
                table: "LabTests");

            migrationBuilder.DropColumn(
                name: "IsApproved",
                table: "LabTests");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Doctors");

            migrationBuilder.AlterColumn<int>(
                name: "PatientId",
                table: "Appointments",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}
