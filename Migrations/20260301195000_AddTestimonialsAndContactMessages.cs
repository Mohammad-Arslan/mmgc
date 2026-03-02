using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MMGC.Migrations
{
    /// <inheritdoc />
    public partial class AddTestimonialsAndContactMessages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create Testimonials table if it doesn't exist
            migrationBuilder.CreateTable(
                name: "Testimonials",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatientId = table.Column<int>(type: "int", nullable: false),
                    DoctorId = table.Column<int>(type: "int", nullable: false),
                    Rating = table.Column<int>(type: "int", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    IsApproved = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Testimonials", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Testimonials_Doctors_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "Doctors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Testimonials_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Create ContactMessages table if it doesn't exist
            migrationBuilder.CreateTable(
                name: "ContactMessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "New"),
                    AdminNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    ResolvedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResolvedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContactMessages", x => x.Id);
                });

            // Create indexes
            migrationBuilder.CreateIndex(
                name: "IX_Testimonials_DoctorId",
                table: "Testimonials",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_Testimonials_PatientId",
                table: "Testimonials",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_ContactMessages_Email",
                table: "ContactMessages",
                column: "Email");

            // Add other missing columns if they don't exist
            // These are safe to add with IF NOT EXISTS logic in SQL
            
            // Add RowVersion to Transactions if it doesn't exist
            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Transactions",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            // Add Findings to Procedures if it doesn't exist
            migrationBuilder.AddColumn<string>(
                name: "Findings",
                table: "Procedures",
                type: "nvarchar(max)",
                maxLength: 5000,
                nullable: true);

            // Add ValidUntil to Prescriptions if it doesn't exist
            migrationBuilder.AddColumn<DateTime>(
                name: "ValidUntil",
                table: "Prescriptions",
                type: "datetime2",
                nullable: true);

            // Add RowVersion to LabTests if it doesn't exist
            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "LabTests",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            // Add ScheduleDate to DoctorSchedules if it doesn't exist
            migrationBuilder.AddColumn<DateTime>(
                name: "ScheduleDate",
                table: "DoctorSchedules",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            // Add UserId to Patients if it doesn't exist
            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "Patients",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            // NOTE: RowVersion on Appointments was already added by Phase2_AddProcedureRequestNotificationAndDocumentModels
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContactMessages");

            migrationBuilder.DropTable(
                name: "Testimonials");

            // NOTE: Do not DropColumn RowVersion from Appointments - it belongs to Phase2 migration

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "ScheduleDate",
                table: "DoctorSchedules");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "LabTests");

            migrationBuilder.DropColumn(
                name: "ValidUntil",
                table: "Prescriptions");

            migrationBuilder.DropColumn(
                name: "Findings",
                table: "Procedures");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Transactions");
        }
    }
}
