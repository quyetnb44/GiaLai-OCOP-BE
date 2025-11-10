using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GiaLaiOCOP.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddMapFeaturesToEnterpriseAndProduct : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 🔹 Products table
            migrationBuilder.Sql(@"
                DO $$ 
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Products' AND column_name = 'AverageRating') THEN
                        ALTER TABLE ""Products"" ADD COLUMN ""AverageRating"" double precision NULL;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Products' AND column_name = 'CreatedAt') THEN
                        ALTER TABLE ""Products"" ADD COLUMN ""CreatedAt"" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Products' AND column_name = 'ImageUrl') THEN
                        ALTER TABLE ""Products"" ADD COLUMN ""ImageUrl"" text NULL;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Products' AND column_name = 'OCOPRating') THEN
                        ALTER TABLE ""Products"" ADD COLUMN ""OCOPRating"" integer NULL;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Products' AND column_name = 'StockStatus') THEN
                        ALTER TABLE ""Products"" ADD COLUMN ""StockStatus"" text NOT NULL DEFAULT '';
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Products' AND column_name = 'UpdatedAt') THEN
                        ALTER TABLE ""Products"" ADD COLUMN ""UpdatedAt"" timestamp with time zone NULL;
                    END IF;
                END $$;
            ");

            // 🔹 Enterprises table
            migrationBuilder.Sql(@"
                DO $$ 
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Enterprises' AND column_name = 'Address') THEN
                        ALTER TABLE ""Enterprises"" ADD COLUMN ""Address"" text NOT NULL DEFAULT '';
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Enterprises' AND column_name = 'AverageRating') THEN
                        ALTER TABLE ""Enterprises"" ADD COLUMN ""AverageRating"" double precision NULL;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Enterprises' AND column_name = 'BusinessField') THEN
                        ALTER TABLE ""Enterprises"" ADD COLUMN ""BusinessField"" text NOT NULL DEFAULT '';
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Enterprises' AND column_name = 'CreatedAt') THEN
                        ALTER TABLE ""Enterprises"" ADD COLUMN ""CreatedAt"" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Enterprises' AND column_name = 'District') THEN
                        ALTER TABLE ""Enterprises"" ADD COLUMN ""District"" text NOT NULL DEFAULT '';
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Enterprises' AND column_name = 'EmailContact') THEN
                        ALTER TABLE ""Enterprises"" ADD COLUMN ""EmailContact"" text NOT NULL DEFAULT '';
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Enterprises' AND column_name = 'ImageUrl') THEN
                        ALTER TABLE ""Enterprises"" ADD COLUMN ""ImageUrl"" text NULL;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Enterprises' AND column_name = 'Latitude') THEN
                        ALTER TABLE ""Enterprises"" ADD COLUMN ""Latitude"" double precision NULL;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Enterprises' AND column_name = 'Longitude') THEN
                        ALTER TABLE ""Enterprises"" ADD COLUMN ""Longitude"" double precision NULL;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Enterprises' AND column_name = 'OCOPRating') THEN
                        ALTER TABLE ""Enterprises"" ADD COLUMN ""OCOPRating"" integer NULL;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Enterprises' AND column_name = 'PhoneNumber') THEN
                        ALTER TABLE ""Enterprises"" ADD COLUMN ""PhoneNumber"" text NOT NULL DEFAULT '';
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Enterprises' AND column_name = 'Province') THEN
                        ALTER TABLE ""Enterprises"" ADD COLUMN ""Province"" text NOT NULL DEFAULT '';
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Enterprises' AND column_name = 'UpdatedAt') THEN
                        ALTER TABLE ""Enterprises"" ADD COLUMN ""UpdatedAt"" timestamp with time zone NULL;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Enterprises' AND column_name = 'Ward') THEN
                        ALTER TABLE ""Enterprises"" ADD COLUMN ""Ward"" text NOT NULL DEFAULT '';
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Enterprises' AND column_name = 'Website') THEN
                        ALTER TABLE ""Enterprises"" ADD COLUMN ""Website"" text NOT NULL DEFAULT '';
                    END IF;
                END $$;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AverageRating",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "OCOPRating",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "StockStatus",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Address",
                table: "Enterprises");

            migrationBuilder.DropColumn(
                name: "AverageRating",
                table: "Enterprises");

            migrationBuilder.DropColumn(
                name: "BusinessField",
                table: "Enterprises");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Enterprises");

            migrationBuilder.DropColumn(
                name: "District",
                table: "Enterprises");

            migrationBuilder.DropColumn(
                name: "EmailContact",
                table: "Enterprises");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "Enterprises");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "Enterprises");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "Enterprises");

            migrationBuilder.DropColumn(
                name: "OCOPRating",
                table: "Enterprises");

            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "Enterprises");

            migrationBuilder.DropColumn(
                name: "Province",
                table: "Enterprises");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Enterprises");

            migrationBuilder.DropColumn(
                name: "Ward",
                table: "Enterprises");

            migrationBuilder.DropColumn(
                name: "Website",
                table: "Enterprises");
        }
    }
}
