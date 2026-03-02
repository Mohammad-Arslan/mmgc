using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MMGC.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUserId1FromPatient : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop UserId1 column and index if they exist (EF created shadow property by mistake)
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Patients_UserId1' AND object_id = OBJECT_ID('Patients'))
                    DROP INDEX [IX_Patients_UserId1] ON [Patients];
                IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Patients') AND name = 'UserId1')
                    ALTER TABLE [Patients] DROP COLUMN [UserId1];
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Re-add if rolling back (not typically needed)
            migrationBuilder.AddColumn<string>(
                name: "UserId1",
                table: "Patients",
                type: "nvarchar(450)",
                nullable: true);
            migrationBuilder.CreateIndex(
                name: "IX_Patients_UserId1",
                table: "Patients",
                column: "UserId1");
        }
    }
}
