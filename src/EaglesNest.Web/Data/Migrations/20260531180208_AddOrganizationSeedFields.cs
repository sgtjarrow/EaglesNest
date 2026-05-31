using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EaglesNest.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationSeedFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Abbreviation",
                table: "OrganizationUnits",
                type: "nvarchar(25)",
                maxLength: 25,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "OrganizationUnits",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StateCode",
                table: "OrganizationUnits",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationUnits_Abbreviation",
                table: "OrganizationUnits",
                column: "Abbreviation",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OrganizationUnits_Abbreviation",
                table: "OrganizationUnits");

            migrationBuilder.DropColumn(
                name: "Abbreviation",
                table: "OrganizationUnits");

            migrationBuilder.DropColumn(
                name: "City",
                table: "OrganizationUnits");

            migrationBuilder.DropColumn(
                name: "StateCode",
                table: "OrganizationUnits");
        }
    }
}
