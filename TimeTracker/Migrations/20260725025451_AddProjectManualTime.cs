using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TimeTracker.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectManualTime : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "ManualTimeSeconds",
                table: "Projects",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Projects_ManualTime_NonNegative",
                table: "Projects",
                sql: "\"ManualTimeSeconds\" >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Projects_ManualTime_NonNegative",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "ManualTimeSeconds",
                table: "Projects");
        }
    }
}
