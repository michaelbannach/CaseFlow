using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using CaseFlow.Domain.Models;

namespace CaseFlow.Infrastructure.Configuration;

public class FormCaseConfiguration : IEntityTypeConfiguration<FormCase>
{
    public void Configure(EntityTypeBuilder<FormCase> builder)
    {
        builder.HasKey(fc => fc.Id);

        builder.Property(fc => fc.Id)
            .ValueGeneratedOnAdd()
            .IsRequired();

        // Enums
        builder.Property(fc => fc.FormType)
            .IsRequired();

        builder.Property(fc => fc.Status)
            .IsRequired();

        // Audit
        builder.Property(fc => fc.CreatedAt)
            .IsRequired();

        builder.Property(fc => fc.UpdatedAt)
            .IsRequired();

        //Only Sachbearbeiter
        builder.HasOne(fc => fc.Department)
            .WithMany()
            .HasForeignKey(fc => fc.DepartmentId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasOne(fc => fc.CreateByEmployee)
            .WithMany()
            .HasForeignKey(fc => fc.CreateByEmployeeId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.Property(fc => fc.ApplicantName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(fc => fc.ApplicantStreet)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(fc => fc.ApplicantZip)
            .IsRequired();

        builder.Property(fc => fc.ApplicantCity)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(fc => fc.ApplicantPhone)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(fc => fc.ApplicantEmail)
            .IsRequired()
            .HasMaxLength(320);
        
        builder.Property(fc => fc.Subject)
            .HasMaxLength(200);

        builder.Property(fc => fc.Notes)
            .HasMaxLength(2000);

        // Leistungsantrag
        builder.Property(fc => fc.ServiceDescription)
            .HasMaxLength(2000);

        builder.Property(fc => fc.Justification)
            .HasMaxLength(2000);

        // Kostenantrag
        builder.Property(fc => fc.Amount)
            .HasPrecision(18, 2);

        builder.Property(fc => fc.CostType)
            .HasMaxLength(100);

        // Organisationsantrag
        builder.Property(fc => fc.ChangeRequest)
            .HasMaxLength(2000);

        // PdfAttachments
        builder.HasMany(fc => fc.Attachments)
            .WithOne(a => a.FormCase)
            .HasForeignKey(a => a.FormCaseId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        // ClarificationMessages
        builder.HasMany(fc => fc.ClarificationMessages)
            .WithOne(m => m.FormCase)
            .HasForeignKey(m => m.FormCaseId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasOne(x => x.CreateByEmployee)
            .WithMany()
            .HasForeignKey(x => x.CreateByEmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ProcessingEmployee)
            .WithMany()
            .HasForeignKey(x => x.ProcessingEmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indizes
        builder.HasIndex(fc => new { fc.DepartmentId, fc.Status });
        builder.HasIndex(fc => new { fc.CreateByEmployeeId, fc.Status });
        builder.HasIndex(fc => fc.FormType);
        builder.HasIndex(fc => fc.CreatedAt);
    }
}