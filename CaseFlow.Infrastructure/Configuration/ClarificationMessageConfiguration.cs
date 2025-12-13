using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using CaseFlow.Domain.Models; // anpassen

namespace CaseFlow.Infrastructure.Configuration;

public class ClarificationMessageConfiguration : IEntityTypeConfiguration<ClarificationMessage>
{
    public void Configure(EntityTypeBuilder<ClarificationMessage> builder)
    {
        builder.HasKey(cm => cm.Id);

        builder.Property(cm => cm.Id)
            .ValueGeneratedOnAdd()
            .IsRequired();

        builder.Property(cm => cm.FormCaseId)
            .IsRequired();

        builder.Property(cm => cm.CreatedByEmployeeId)
            .IsRequired();

        builder.Property(cm => cm.Message)
            .IsRequired()
            .HasMaxLength(2000);

      // Relation to FormCase (1:n)
        builder.HasOne(cm => cm.FormCase)
            .WithMany(fc => fc.ClarificationMessages)
            .HasForeignKey(cm => cm.FormCaseId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        // Relation to Employee (Creator of ClarificationRequest)
        builder.HasOne(cm => cm.CreatedByEmployee)
            .WithMany()
            .HasForeignKey(cm => cm.CreatedByEmployeeId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasIndex(cm => cm.FormCaseId);
        
    }
}