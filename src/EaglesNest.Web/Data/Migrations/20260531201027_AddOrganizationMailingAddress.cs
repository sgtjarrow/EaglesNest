using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EaglesNest.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationMailingAddress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MailingAddressLine1",
                table: "OrganizationUnits",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MailingAddressLine2",
                table: "OrganizationUnits",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MailingCity",
                table: "OrganizationUnits",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MailingPostalCode",
                table: "OrganizationUnits",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MailingStateCode",
                table: "OrganizationUnits",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MailingAddressLine1",
                table: "OrganizationUnits");

            migrationBuilder.DropColumn(
                name: "MailingAddressLine2",
                table: "OrganizationUnits");

            migrationBuilder.DropColumn(
                name: "MailingCity",
                table: "OrganizationUnits");

            migrationBuilder.DropColumn(
                name: "MailingPostalCode",
                table: "OrganizationUnits");

            migrationBuilder.DropColumn(
                name: "MailingStateCode",
                table: "OrganizationUnits");
        }
    }
}
