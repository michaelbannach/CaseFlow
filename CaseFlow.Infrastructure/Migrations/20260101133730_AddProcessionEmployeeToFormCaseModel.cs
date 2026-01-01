using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CaseFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProcessionEmployeeToFormCaseModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProcessingEmployeeId",
                table: "FormCases",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_FormCases_ProcessingEmployeeId",
                table: "FormCases",
                column: "ProcessingEmployeeId");

            migrationBuilder.AddForeignKey(
                name: "FK_FormCases_Employees_ProcessingEmployeeId",
                table: "FormCases",
                column: "ProcessingEmployeeId",
                principalTable: "Employees",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FormCases_Employees_ProcessingEmployeeId",
                table: "FormCases");

            migrationBuilder.DropIndex(
                name: "IX_FormCases_ProcessingEmployeeId",
                table: "FormCases");

            migrationBuilder.DropColumn(
                name: "ProcessingEmployeeId",
                table: "FormCases");
        }
    }
}
