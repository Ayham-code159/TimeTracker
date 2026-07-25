using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TimeTracker.Migrations
{
    /// <inheritdoc />
    public partial class TrackManualTimeAdjustments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ManualTimeAdjustments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DurationSeconds = table.Column<long>(type: "bigint", nullable: false),
                    AddedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProjectId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ManualTimeAdjustments", x => x.Id);
                    table.CheckConstraint("CK_ManualTimeAdjustments_Duration_Positive", "\"DurationSeconds\" > 0");
                    table.ForeignKey(
                        name: "FK_ManualTimeAdjustments_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ManualTimeAdjustments_ProjectId",
                table: "ManualTimeAdjustments",
                column: "ProjectId");

            migrationBuilder.Sql(
                """
                INSERT INTO "ManualTimeAdjustments" ("DurationSeconds", "AddedAtUtc", "ProjectId")
                SELECT "ManualTimeSeconds", CURRENT_TIMESTAMP, "Id"
                FROM "Projects"
                WHERE "ManualTimeSeconds" > 0
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ManualTimeAdjustments");
        }
    }
}
