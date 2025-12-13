using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CaseFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ChangedNameofAttachmentsToPdfAttachments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Attachments_Employees_UploadedByEmployeeId",
                table: "Attachments");

            migrationBuilder.DropForeignKey(
                name: "FK_Attachments_FormCases_FormCaseId",
                table: "Attachments");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Attachments",
                table: "Attachments");

            migrationBuilder.RenameTable(
                name: "Attachments",
                newName: "PdfAttachments");

            migrationBuilder.RenameIndex(
                name: "IX_Attachments_UploadedByEmployeeId",
                table: "PdfAttachments",
                newName: "IX_PdfAttachments_UploadedByEmployeeId");

            migrationBuilder.RenameIndex(
                name: "IX_Attachments_FormCaseId_UploadedAt",
                table: "PdfAttachments",
                newName: "IX_PdfAttachments_FormCaseId_UploadedAt");

            migrationBuilder.RenameIndex(
                name: "IX_Attachments_FormCaseId",
                table: "PdfAttachments",
                newName: "IX_PdfAttachments_FormCaseId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PdfAttachments",
                table: "PdfAttachments",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PdfAttachments_Employees_UploadedByEmployeeId",
                table: "PdfAttachments",
                column: "UploadedByEmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PdfAttachments_FormCases_FormCaseId",
                table: "PdfAttachments",
                column: "FormCaseId",
                principalTable: "FormCases",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PdfAttachments_Employees_UploadedByEmployeeId",
                table: "PdfAttachments");

            migrationBuilder.DropForeignKey(
                name: "FK_PdfAttachments_FormCases_FormCaseId",
                table: "PdfAttachments");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PdfAttachments",
                table: "PdfAttachments");

            migrationBuilder.RenameTable(
                name: "PdfAttachments",
                newName: "Attachments");

            migrationBuilder.RenameIndex(
                name: "IX_PdfAttachments_UploadedByEmployeeId",
                table: "Attachments",
                newName: "IX_Attachments_UploadedByEmployeeId");

            migrationBuilder.RenameIndex(
                name: "IX_PdfAttachments_FormCaseId_UploadedAt",
                table: "Attachments",
                newName: "IX_Attachments_FormCaseId_UploadedAt");

            migrationBuilder.RenameIndex(
                name: "IX_PdfAttachments_FormCaseId",
                table: "Attachments",
                newName: "IX_Attachments_FormCaseId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Attachments",
                table: "Attachments",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Attachments_Employees_UploadedByEmployeeId",
                table: "Attachments",
                column: "UploadedByEmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Attachments_FormCases_FormCaseId",
                table: "Attachments",
                column: "FormCaseId",
                principalTable: "FormCases",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
