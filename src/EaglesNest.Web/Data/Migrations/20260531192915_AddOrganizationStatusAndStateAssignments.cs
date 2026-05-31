using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EaglesNest.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationStatusAndStateAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "CharterDate",
                table: "OrganizationUnits",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "OrganizationUnits",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.Sql("""
                UPDATE OrganizationUnits
                SET Status = CASE WHEN IsActive = 1 THEN 1 ELSE 3 END
                """);

            migrationBuilder.DropColumn(
                name: "CharterNumber",
                table: "OrganizationUnits");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "OrganizationUnits");

            migrationBuilder.AddColumn<string>(
                name: "ActorName",
                table: "AuditLogs",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "Legacy");

            migrationBuilder.AddColumn<string>(
                name: "ActorSource",
                table: "AuditLogs",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "Legacy");

            migrationBuilder.CreateTable(
                name: "ChapterSuspensions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StartsOn = table.Column<DateOnly>(type: "date", nullable: false),
                    EndsOn = table.Column<DateOnly>(type: "date", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ActorName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ActorSource = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChapterSuspensions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChapterSuspensions_OrganizationUnits_OrganizationUnitId",
                        column: x => x.OrganizationUnitId,
                        principalTable: "OrganizationUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StateChapterAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StateOrganizationUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LocalChapterOrganizationUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StartsOn = table.Column<DateOnly>(type: "date", nullable: false),
                    EndsOn = table.Column<DateOnly>(type: "date", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ActorName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ActorSource = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StateChapterAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StateChapterAssignments_OrganizationUnits_LocalChapterOrganizationUnitId",
                        column: x => x.LocalChapterOrganizationUnitId,
                        principalTable: "OrganizationUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StateChapterAssignments_OrganizationUnits_StateOrganizationUnitId",
                        column: x => x.StateOrganizationUnitId,
                        principalTable: "OrganizationUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChapterSuspensions_OrganizationUnitId",
                table: "ChapterSuspensions",
                column: "OrganizationUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_StateChapterAssignments_LocalChapterOrganizationUnitId",
                table: "StateChapterAssignments",
                column: "LocalChapterOrganizationUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_StateChapterAssignments_StateOrganizationUnitId",
                table: "StateChapterAssignments",
                column: "StateOrganizationUnitId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChapterSuspensions");

            migrationBuilder.DropTable(
                name: "StateChapterAssignments");

            migrationBuilder.DropColumn(
                name: "CharterDate",
                table: "OrganizationUnits");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "OrganizationUnits");

            migrationBuilder.DropColumn(
                name: "ActorName",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "ActorSource",
                table: "AuditLogs");

            migrationBuilder.AddColumn<string>(
                name: "CharterNumber",
                table: "OrganizationUnits",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "OrganizationUnits",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
