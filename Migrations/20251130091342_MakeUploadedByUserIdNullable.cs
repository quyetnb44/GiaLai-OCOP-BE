using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GiaLaiOCOP.Api.Migrations
{
    /// <inheritdoc />
    public partial class MakeUploadedByUserIdNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Images_Users_UploadedByUserId",
                table: "Images");

            migrationBuilder.AlterColumn<int>(
                name: "UploadedByUserId",
                table: "Images",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddForeignKey(
                name: "FK_Images_Users_UploadedByUserId",
                table: "Images",
                column: "UploadedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Images_Users_UploadedByUserId",
                table: "Images");

            migrationBuilder.AlterColumn<int>(
                name: "UploadedByUserId",
                table: "Images",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Images_Users_UploadedByUserId",
                table: "Images",
                column: "UploadedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
