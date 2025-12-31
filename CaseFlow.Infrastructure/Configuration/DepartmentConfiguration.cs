using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using CaseFlow.Domain.Models;

namespace CaseFlow.Infrastructure.Configuration;

public class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.HasKey(d => d.Id);
        
        builder.Property(d => d.Name)
            .IsRequired()
            .HasMaxLength(50);
        
        builder.HasIndex(d => d.Name)
            .IsUnique();
        
        builder.HasData(
            new Department { Id = 1, Name = "Allgemein" },
            new Department { Id = 2, Name = "Leistungen" },
            new Department { Id = 3, Name = "Kosten" },
            new Department { Id = 4, Name = "Organisation" }
        );
        
    }
}