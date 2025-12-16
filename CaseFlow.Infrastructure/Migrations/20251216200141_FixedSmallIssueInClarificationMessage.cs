using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CaseFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixedSmallIssueInClarificationMessage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ClarificationMessages_Employees_CreatedByEmployeeId",
                table: "ClarificationMessages");

            migrationBuilder.RenameColumn(
                name: "CreatedOAt",
                table: "ClarificationMessages",
                newName: "CreatedAt");

            migrationBuilder.AlterColumn<int>(
                name: "DepartmentId",
                table: "Employees",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Message",
                table: "ClarificationMessages",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddForeignKey(
                name: "FK_ClarificationMessages_Employees_CreatedByEmployeeId",
                table: "ClarificationMessages",
                column: "CreatedByEmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ClarificationMessages_Employees_CreatedByEmployeeId",
                table: "ClarificationMessages");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "ClarificationMessages",
                newName: "CreatedOAt");

            migrationBuilder.AlterColumn<int>(
                name: "DepartmentId",
                table: "Employees",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "Message",
                table: "ClarificationMessages",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(2000)",
                oldMaxLength: 2000);

            migrationBuilder.AddForeignKey(
                name: "FK_ClarificationMessages_Employees_CreatedByEmployeeId",
                table: "ClarificationMessages",
                column: "CreatedByEmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
