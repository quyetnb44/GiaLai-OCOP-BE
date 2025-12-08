using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GiaLaiOCOP.Api.Migrations
{
    /// <inheritdoc />
    public partial class EnsureAllUsersHaveWallets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Tự động tạo ví cho tất cả user chưa có ví
            migrationBuilder.Sql(@"
                INSERT INTO ""Wallets"" (""UserId"", ""Balance"", ""Currency"", ""CreatedAt"")
                SELECT 
                    ""Id"",
                    0,
                    'VND',
                    NOW()
                FROM ""Users""
                WHERE ""Id"" NOT IN (
                    SELECT DISTINCT ""UserId"" 
                    FROM ""Wallets""
                    WHERE ""UserId"" IS NOT NULL
                );
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Không cần rollback vì đây là migration một chiều
            // Nếu cần rollback, có thể xóa ví của user không có giao dịch
            // Nhưng không nên làm vì có thể mất dữ liệu
        }
    }
}
