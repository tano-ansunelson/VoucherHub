using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VoucherHub.Migrations
{
    /// <inheritdoc />
    public partial class AddBeceWassce : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Vouchers_Status",
                table: "Vouchers");

            migrationBuilder.AddColumn<string>(
                name: "ExamType",
                table: "Vouchers",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ExamType",
                table: "Purchases",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Vouchers_ExamType_Status",
                table: "Vouchers",
                columns: new[] { "ExamType", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Vouchers_ExamType_Status",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "ExamType",
                table: "Vouchers");

            migrationBuilder.DropColumn(
                name: "ExamType",
                table: "Purchases");

            migrationBuilder.CreateIndex(
                name: "IX_Vouchers_Status",
                table: "Vouchers",
                column: "Status");
        }
    }
}
