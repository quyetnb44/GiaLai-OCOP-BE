using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GiaLaiOCOP.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEnterprisePaymentSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Thêm EnterpriseId nullable trước
            migrationBuilder.AddColumn<int>(
                name: "EnterpriseId",
                table: "Payments",
                type: "integer",
                nullable: true);

            // Cập nhật EnterpriseId từ OrderItems nếu có dữ liệu
            migrationBuilder.Sql(@"
                UPDATE ""Payments"" p
                SET ""EnterpriseId"" = (
                    SELECT DISTINCT pr.""EnterpriseId""
                    FROM ""OrderItems"" oi
                    INNER JOIN ""Products"" pr ON oi.""ProductId"" = pr.""Id""
                    WHERE oi.""OrderId"" = p.""OrderId""
                    LIMIT 1
                )
                WHERE EXISTS (
                    SELECT 1
                    FROM ""OrderItems"" oi
                    INNER JOIN ""Products"" pr ON oi.""ProductId"" = pr.""Id""
                    WHERE oi.""OrderId"" = p.""OrderId""
                );
            ");

            // Xóa các payments không có enterprise (nếu có)
            migrationBuilder.Sql(@"
                DELETE FROM ""Payments""
                WHERE ""EnterpriseId"" IS NULL;
            ");

            // Đổi EnterpriseId thành NOT NULL
            migrationBuilder.AlterColumn<int>(
                name: "EnterpriseId",
                table: "Payments",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BankAccount",
                table: "Enterprises",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BankAccountName",
                table: "Enterprises",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BankCode",
                table: "Enterprises",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_EnterpriseId",
                table: "Payments",
                column: "EnterpriseId");

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_Enterprises_EnterpriseId",
                table: "Payments",
                column: "EnterpriseId",
                principalTable: "Enterprises",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Payments_Enterprises_EnterpriseId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_EnterpriseId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "EnterpriseId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "BankAccount",
                table: "Enterprises");

            migrationBuilder.DropColumn(
                name: "BankAccountName",
                table: "Enterprises");

            migrationBuilder.DropColumn(
                name: "BankCode",
                table: "Enterprises");
        }
    }
}
