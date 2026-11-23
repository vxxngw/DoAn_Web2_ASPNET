using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Recruitment.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddJobSalary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "MinSalary",
                table: "Jobs",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MaxSalary",
                table: "Jobs",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MinSalary",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "MaxSalary",
                table: "Jobs");
        }
    }
}
