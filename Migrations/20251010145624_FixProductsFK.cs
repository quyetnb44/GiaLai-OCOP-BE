using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GiaLaiOCOP.Api.Migrations
{
    /// <inheritdoc />
    public partial class FixProductsFK : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Bỏ FK cũ
            migrationBuilder.DropForeignKey(
                name: "FK_Products_Enterprises_EnterpriseId",
                table: "Products");

            // 🔹 Thêm enterprise mặc định nếu chưa có
            migrationBuilder.Sql(@"
        INSERT INTO ""Enterprises"" (""Name"")
        SELECT 'Default Enterprise'
        WHERE NOT EXISTS (SELECT 1 FROM ""Enterprises"" WHERE ""Id"" = 1);
    ");

            // 🔹 Cập nhật Products trỏ tới Enterprise thực sự tồn tại
            migrationBuilder.Sql(@"
        UPDATE ""Products""
        SET ""EnterpriseId"" = 1
        WHERE ""EnterpriseId"" IS NULL;
    ");

            // Thay đổi cột EnterpriseId
            migrationBuilder.AlterColumn<int>(
                name: "EnterpriseId",
                table: "Products",
                type: "integer",
                nullable: false,
                defaultValue: 1, // default enterprise Id phải tồn tại
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            // Tạo FK mới
            migrationBuilder.AddForeignKey(
                name: "FK_Products_Enterprises_EnterpriseId",
                table: "Products",
                column: "EnterpriseId",
                principalTable: "Enterprises",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }


        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Products_Enterprises_EnterpriseId",
                table: "Products");

            migrationBuilder.AlterColumn<int>(
                name: "EnterpriseId",
                table: "Products",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddForeignKey(
                name: "FK_Products_Enterprises_EnterpriseId",
                table: "Products",
                column: "EnterpriseId",
                principalTable: "Enterprises",
                principalColumn: "Id");
        }
    }
}
