using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EaglesNest.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class SystemAdminMemberFlag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSystemAdmin",
                table: "Members",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql(
                """
                UPDATE Members
                SET IsSystemAdmin = 1
                WHERE Id IN (
                    SELECT MemberId
                    FROM RoleAssignments
                    WHERE Position = 1
                );

                DELETE FROM RoleAssignments
                WHERE Position = 1;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsSystemAdmin",
                table: "Members");
        }
    }
}
