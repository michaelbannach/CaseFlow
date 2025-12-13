using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using CaseFlow.Domain.Models;

namespace CaseFlow.Infrastructure.Configuration;

public class PdfAttachmentConfiguration : IEntityTypeConfiguration<PdfAttachment>
{
    public void Configure(EntityTypeBuilder<PdfAttachment> builder)
    {
        builder.HasKey(pa => pa.Id);

        builder.Property(pa => pa.Id)
            .ValueGeneratedOnAdd()
            .IsRequired();

        builder.Property(pa => pa.FormCaseId)
            .IsRequired();

        builder.Property(pa => pa.FileName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(pa => pa.ContentType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(pa => pa.SizeBytes)
            .IsRequired();

        builder.Property(pa => pa.StorageKey)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(pa => pa.UploadedByEmployeeId)
            .IsRequired();

        builder.Property(pa => pa.UploadedAt)
            .IsRequired();

        // Relation FormCase (1:n)
        builder.HasOne(pa => pa.FormCase)
            .WithMany(fc => fc.Attachments)
            .HasForeignKey(pa => pa.FormCaseId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        // Relation Employee (Uploader)
        builder.HasOne(pa => pa.UploadedByEmployee)
            .WithMany()
            .HasForeignKey(pa => pa.UploadedByEmployeeId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        //Attachments to case, sort by uploaddate
        builder.HasIndex(pa => pa.FormCaseId);
        builder.HasIndex(pa => new { pa.FormCaseId, pa.UploadedAt });
    }
}