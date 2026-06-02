using Microsoft.EntityFrameworkCore.Migrations;

using System;

#nullable disable

namespace EaglesNest.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class MemberStatusFlowCleanup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BloodType",
                table: "Members",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Gender",
                table: "Members",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "LifetimeDate",
                table: "Members",
                type: "date",
                nullable: true);

            migrationBuilder.DropColumn(
                name: "JoinedOn",
                table: "Members");

            migrationBuilder.AddColumn<string>(
                name: "ConflictTab",
                table: "MilitaryServiceRecords",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DischargeType",
                table: "MilitaryServiceRecords",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MemberStatusHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    EffectiveDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ActorName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ActorSource = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemberStatusHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MemberStatusHistory_Members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MemberStatusHistory_MemberId",
                table: "MemberStatusHistory",
                column: "MemberId");

            migrationBuilder.Sql("UPDATE Members SET Status = 4 WHERE Status IN (6, 7);");

            migrationBuilder.Sql("""
                INSERT INTO MemberStatusHistory (Id, MemberId, Status, EffectiveDate, Notes, CreatedByUserId, ActorName, ActorSource, CreatedAt)
                SELECT NEWID(), Id, Status, CAST(CreatedAt AS date), 'Initial status history created during member status flow migration.', NULL, 'MemberStatusFlowCleanup', 'Migration', SYSUTCDATETIME()
                FROM Members
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM MemberStatusHistory
                    WHERE MemberStatusHistory.MemberId = Members.Id
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MemberStatusHistory");

            migrationBuilder.DropColumn(
                name: "BloodType",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "Gender",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "LifetimeDate",
                table: "Members");

            migrationBuilder.AddColumn<DateOnly>(
                name: "JoinedOn",
                table: "Members",
                type: "date",
                nullable: true);

            migrationBuilder.DropColumn(
                name: "ConflictTab",
                table: "MilitaryServiceRecords");

            migrationBuilder.DropColumn(
                name: "DischargeType",
                table: "MilitaryServiceRecords");
        }
    }
}
