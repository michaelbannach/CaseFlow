using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CaseFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddClarificationMessages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FormCases_Employees_ProcessingEmployeeId",
                table: "FormCases");

            migrationBuilder.AddForeignKey(
                name: "FK_FormCases_Employees_ProcessingEmployeeId",
                table: "FormCases",
                column: "ProcessingEmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FormCases_Employees_ProcessingEmployeeId",
                table: "FormCases");

            migrationBuilder.AddForeignKey(
                name: "FK_FormCases_Employees_ProcessingEmployeeId",
                table: "FormCases",
                column: "ProcessingEmployeeId",
                principalTable: "Employees",
                principalColumn: "Id");
        }
    }
}
