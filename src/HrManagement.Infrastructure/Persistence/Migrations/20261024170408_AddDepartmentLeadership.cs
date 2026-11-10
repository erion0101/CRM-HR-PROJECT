using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HrManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDepartmentLeadership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "HrEmployeeId",
                table: "Departments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ProductManagerEmployeeId",
                table: "Departments",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Departments_HrEmployeeId",
                table: "Departments",
                column: "HrEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_Departments_ProductManagerEmployeeId",
                table: "Departments",
                column: "ProductManagerEmployeeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Departments_Employees_HrEmployeeId",
                table: "Departments",
                column: "HrEmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Departments_Employees_ProductManagerEmployeeId",
                table: "Departments",
                column: "ProductManagerEmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Departments_Employees_HrEmployeeId",
                table: "Departments");

            migrationBuilder.DropForeignKey(
                name: "FK_Departments_Employees_ProductManagerEmployeeId",
                table: "Departments");

            migrationBuilder.DropIndex(
                name: "IX_Departments_HrEmployeeId",
                table: "Departments");

            migrationBuilder.DropIndex(
                name: "IX_Departments_ProductManagerEmployeeId",
                table: "Departments");

            migrationBuilder.DropColumn(
                name: "HrEmployeeId",
                table: "Departments");

            migrationBuilder.DropColumn(
                name: "ProductManagerEmployeeId",
                table: "Departments");
        }
    }
}
