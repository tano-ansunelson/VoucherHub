using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VoucherHub.Migrations
{
    /// <inheritdoc />
    public partial class RemoveIdFromPurchaseVoucher : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PurchaseVoucherId",
                table: "PurchaseVouchers");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PurchaseVoucherId",
                table: "PurchaseVouchers",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
