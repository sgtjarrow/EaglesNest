using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EaglesNest.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class MemberBasedOfficerRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "MemberId",
                table: "RoleAssignments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.Sql(
                """
                DELETE FROM RoleAssignments
                WHERE Position IN (2, 3, 10, 11);

                UPDATE RoleAssignments
                SET Position = CASE Position
                    WHEN 4 THEN 2
                    WHEN 5 THEN 3
                    WHEN 6 THEN 4
                    WHEN 7 THEN 5
                    WHEN 9 THEN 6
                    WHEN 8 THEN 7
                    ELSE Position
                END;

                UPDATE roleAssignment
                SET MemberId = member.Id
                FROM RoleAssignments roleAssignment
                INNER JOIN Members member ON member.ApplicationUserId = roleAssignment.ApplicationUserId;

                UPDATE roleAssignment
                SET OrganizationUnitId = member.PrimaryChapterId
                FROM RoleAssignments roleAssignment
                INNER JOIN Members member ON member.Id = roleAssignment.MemberId
                WHERE roleAssignment.OrganizationUnitId IS NULL;

                DELETE FROM RoleAssignments
                WHERE MemberId IS NULL;
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "MemberId",
                table: "RoleAssignments",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "OrganizationUnitId",
                table: "RoleAssignments",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "ApplicationUserId",
                table: "RoleAssignments");

            migrationBuilder.CreateIndex(
                name: "IX_RoleAssignments_MemberId_OrganizationUnitId_Position",
                table: "RoleAssignments",
                columns: new[] { "MemberId", "OrganizationUnitId", "Position" },
                unique: true,
                filter: "[OrganizationUnitId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_RoleAssignments_Members_MemberId",
                table: "RoleAssignments",
                column: "MemberId",
                principalTable: "Members",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RoleAssignments_Members_MemberId",
                table: "RoleAssignments");

            migrationBuilder.DropIndex(
                name: "IX_RoleAssignments_MemberId_OrganizationUnitId_Position",
                table: "RoleAssignments");

            migrationBuilder.DropColumn(
                name: "MemberId",
                table: "RoleAssignments");

            migrationBuilder.AlterColumn<Guid>(
                name: "OrganizationUnitId",
                table: "RoleAssignments",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<string>(
                name: "ApplicationUserId",
                table: "RoleAssignments",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(
                """
                UPDATE roleAssignment
                SET ApplicationUserId = member.ApplicationUserId
                FROM RoleAssignments roleAssignment
                INNER JOIN Members member ON member.Id = roleAssignment.MemberId
                WHERE member.ApplicationUserId IS NOT NULL;
                """);
        }
    }
}
