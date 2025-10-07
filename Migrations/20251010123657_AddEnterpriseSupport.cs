using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GiaLaiOCOP.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddEnterpriseSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EnterpriseId",
                table: "Users",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EnterpriseId",
                table: "Products",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Enterprises",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Enterprises", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_EnterpriseId",
                table: "Users",
                column: "EnterpriseId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_EnterpriseId",
                table: "Products",
                column: "EnterpriseId");

            migrationBuilder.AddForeignKey(
                name: "FK_Products_Enterprises_EnterpriseId",
                table: "Products",
                column: "EnterpriseId",
                principalTable: "Enterprises",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Enterprises_EnterpriseId",
                table: "Users",
                column: "EnterpriseId",
                principalTable: "Enterprises",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Products_Enterprises_EnterpriseId",
                table: "Products");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Enterprises_EnterpriseId",
                table: "Users");

            migrationBuilder.DropTable(
                name: "Enterprises");

            migrationBuilder.DropIndex(
                name: "IX_Users_EnterpriseId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Products_EnterpriseId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "EnterpriseId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "EnterpriseId",
                table: "Products");
        }
    }
}
