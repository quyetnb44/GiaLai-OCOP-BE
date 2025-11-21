using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GiaLaiOCOP.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddUserProfileFieldsToUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Sử dụng SQL để kiểm tra và thêm cột nếu chưa tồn tại
            migrationBuilder.Sql(@"
                DO $$ 
                BEGIN
                    -- Kiểm tra và thêm AvatarUrl nếu chưa tồn tại
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Users' AND column_name = 'AvatarUrl') THEN
                        ALTER TABLE ""Users"" ADD COLUMN ""AvatarUrl"" text NULL;
                    END IF;

                    -- Kiểm tra và thêm DateOfBirth nếu chưa tồn tại
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Users' AND column_name = 'DateOfBirth') THEN
                        ALTER TABLE ""Users"" ADD COLUMN ""DateOfBirth"" timestamp with time zone NULL;
                    END IF;

                    -- Kiểm tra và thêm Gender nếu chưa tồn tại
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Users' AND column_name = 'Gender') THEN
                        ALTER TABLE ""Users"" ADD COLUMN ""Gender"" text NULL;
                    END IF;

                    -- Kiểm tra và thêm PhoneNumber nếu chưa tồn tại
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Users' AND column_name = 'PhoneNumber') THEN
                        ALTER TABLE ""Users"" ADD COLUMN ""PhoneNumber"" text NULL;
                    END IF;

                    -- Kiểm tra và thêm ShippingAddress nếu chưa tồn tại
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Users' AND column_name = 'ShippingAddress') THEN
                        ALTER TABLE ""Users"" ADD COLUMN ""ShippingAddress"" text NULL;
                    END IF;

                    -- Kiểm tra và thêm UpdatedAt nếu chưa tồn tại
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Users' AND column_name = 'UpdatedAt') THEN
                        ALTER TABLE ""Users"" ADD COLUMN ""UpdatedAt"" timestamp with time zone NULL;
                    END IF;
                END $$;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AvatarUrl",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DateOfBirth",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Gender",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ShippingAddress",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Users");
        }
    }
}
