using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Recruitment.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddIndustriesAndSavedJobs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Industry",
                table: "Jobs",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            // Chỉ phân ngành các vị trí đã biết trong bộ dữ liệu mẫu.
            migrationBuilder.Sql("""
                UPDATE j SET Industry = CASE
                    WHEN j.Title IN (N'Lập trình viên Backend C#', N'Thực tập sinh Frontend', N'Kiểm thử viên phần mềm', N'Kỹ sư dữ liệu', N'Chuyên viên hỗ trợ kỹ thuật', N'Chuyên viên phân tích nghiệp vụ') THEN N'Công nghệ thông tin'
                    WHEN j.Title IN (N'Thiết kế UI/UX', N'Thiết kế sản phẩm số') THEN N'Thiết kế'
                    WHEN j.Title IN (N'Nhân viên kinh doanh', N'Chuyên viên thương mại điện tử', N'Nhân viên phát triển thị trường') THEN N'Kinh doanh & thương mại'
                    WHEN j.Title IN (N'Trợ lý thu mua', N'Nhân viên kho vận') THEN N'Logistics & thu mua'
                    WHEN j.Title IN (N'Chuyên viên marketing', N'Chuyên viên sáng tạo nội dung', N'Nhân viên tổ chức sự kiện') THEN N'Marketing & truyền thông'
                    WHEN j.Title IN (N'Kế toán bán hàng') THEN N'Kế toán'
                    WHEN j.Title IN (N'Điều phối viên dự án', N'Trợ lý vận hành') THEN N'Quản lý & vận hành'
                    WHEN j.Title IN (N'Chuyên viên chăm sóc khách hàng') THEN N'Chăm sóc khách hàng'
                    WHEN j.Title IN (N'Nhân viên hành chính', N'Chuyên viên tuyển dụng', N'Chuyên viên đào tạo') THEN N'Hành chính & nhân sự'
                END
                FROM Jobs j JOIN Users u ON j.EmployerId = u.Id
                WHERE u.Email LIKE '%@seed500.test' OR u.Email LIKE 'company-%@demo.invalid'
                """);

            migrationBuilder.CreateTable(
                name: "SavedJobs",
                columns: table => new
                {
                    CandidateId = table.Column<int>(type: "int", nullable: false),
                    JobId = table.Column<int>(type: "int", nullable: false),
                    SavedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedJobs", x => new { x.CandidateId, x.JobId });
                    table.ForeignKey(
                        name: "FK_SavedJobs_Jobs_JobId",
                        column: x => x.JobId,
                        principalTable: "Jobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SavedJobs_Users_CandidateId",
                        column: x => x.CandidateId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SavedJobs_JobId",
                table: "SavedJobs",
                column: "JobId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SavedJobs");

            migrationBuilder.DropColumn(
                name: "Industry",
                table: "Jobs");
        }
    }
}
