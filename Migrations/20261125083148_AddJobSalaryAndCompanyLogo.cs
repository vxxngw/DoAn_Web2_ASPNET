using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Recruitment.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddJobSalaryAndCompanyLogo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "CompanyLogo",
                table: "Users",
                type: "varbinary(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsSalaryNegotiable",
                table: "Jobs",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SalaryMax",
                table: "Jobs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SalaryMin",
                table: "Jobs",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompanyLogo",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsSalaryNegotiable",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "SalaryMax",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "SalaryMin",
                table: "Jobs");
        }
    }
}
