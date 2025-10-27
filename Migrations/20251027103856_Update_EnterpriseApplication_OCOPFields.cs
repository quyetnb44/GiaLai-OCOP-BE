using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GiaLaiOCOP.Api.Migrations
{
    /// <inheritdoc />
    public partial class Update_EnterpriseApplication_OCOPFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Description",
                table: "EnterpriseApplications",
                newName: "Website");

            migrationBuilder.AddColumn<string>(
                name: "AdditionalNotes",
                table: "EnterpriseApplications",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AdminComment",
                table: "EnterpriseApplications",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AttachedDocuments",
                table: "EnterpriseApplications",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BusinessField",
                table: "EnterpriseApplications",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BusinessLicenseNumber",
                table: "EnterpriseApplications",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "District",
                table: "EnterpriseApplications",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LicenseIssuedBy",
                table: "EnterpriseApplications",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "LicenseIssuedDate",
                table: "EnterpriseApplications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NumberOfEmployees",
                table: "EnterpriseApplications",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ProductCategory",
                table: "EnterpriseApplications",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ProductCertifications",
                table: "EnterpriseApplications",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ProductDescription",
                table: "EnterpriseApplications",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ProductImages",
                table: "EnterpriseApplications",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ProductName",
                table: "EnterpriseApplications",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ProductOrigin",
                table: "EnterpriseApplications",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ProductionLocation",
                table: "EnterpriseApplications",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ProductionScale",
                table: "EnterpriseApplications",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Province",
                table: "EnterpriseApplications",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RepresentativeIdIssuedBy",
                table: "EnterpriseApplications",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "RepresentativeIdIssuedDate",
                table: "EnterpriseApplications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RepresentativeIdNumber",
                table: "EnterpriseApplications",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RepresentativeName",
                table: "EnterpriseApplications",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RepresentativePosition",
                table: "EnterpriseApplications",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "EnterpriseApplications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Ward",
                table: "EnterpriseApplications",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdditionalNotes",
                table: "EnterpriseApplications");

            migrationBuilder.DropColumn(
                name: "AdminComment",
                table: "EnterpriseApplications");

            migrationBuilder.DropColumn(
                name: "AttachedDocuments",
                table: "EnterpriseApplications");

            migrationBuilder.DropColumn(
                name: "BusinessField",
                table: "EnterpriseApplications");

            migrationBuilder.DropColumn(
                name: "BusinessLicenseNumber",
                table: "EnterpriseApplications");

            migrationBuilder.DropColumn(
                name: "District",
                table: "EnterpriseApplications");

            migrationBuilder.DropColumn(
                name: "LicenseIssuedBy",
                table: "EnterpriseApplications");

            migrationBuilder.DropColumn(
                name: "LicenseIssuedDate",
                table: "EnterpriseApplications");

            migrationBuilder.DropColumn(
                name: "NumberOfEmployees",
                table: "EnterpriseApplications");

            migrationBuilder.DropColumn(
                name: "ProductCategory",
                table: "EnterpriseApplications");

            migrationBuilder.DropColumn(
                name: "ProductCertifications",
                table: "EnterpriseApplications");

            migrationBuilder.DropColumn(
                name: "ProductDescription",
                table: "EnterpriseApplications");

            migrationBuilder.DropColumn(
                name: "ProductImages",
                table: "EnterpriseApplications");

            migrationBuilder.DropColumn(
                name: "ProductName",
                table: "EnterpriseApplications");

            migrationBuilder.DropColumn(
                name: "ProductOrigin",
                table: "EnterpriseApplications");

            migrationBuilder.DropColumn(
                name: "ProductionLocation",
                table: "EnterpriseApplications");

            migrationBuilder.DropColumn(
                name: "ProductionScale",
                table: "EnterpriseApplications");

            migrationBuilder.DropColumn(
                name: "Province",
                table: "EnterpriseApplications");

            migrationBuilder.DropColumn(
                name: "RepresentativeIdIssuedBy",
                table: "EnterpriseApplications");

            migrationBuilder.DropColumn(
                name: "RepresentativeIdIssuedDate",
                table: "EnterpriseApplications");

            migrationBuilder.DropColumn(
                name: "RepresentativeIdNumber",
                table: "EnterpriseApplications");

            migrationBuilder.DropColumn(
                name: "RepresentativeName",
                table: "EnterpriseApplications");

            migrationBuilder.DropColumn(
                name: "RepresentativePosition",
                table: "EnterpriseApplications");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "EnterpriseApplications");

            migrationBuilder.DropColumn(
                name: "Ward",
                table: "EnterpriseApplications");

            migrationBuilder.RenameColumn(
                name: "Website",
                table: "EnterpriseApplications",
                newName: "Description");
        }
    }
}
