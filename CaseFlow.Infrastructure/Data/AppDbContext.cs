using CaseFlow.Domain.Models;
using CaseFlow.Infrastructure.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

using CaseFlow.Infrastructure.Models;

namespace CaseFlow.Infrastructure.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
        
    }
    
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<FormCase> FormCases => Set<FormCase>();
    public DbSet<ClarificationMessage> ClarificationMessages => Set<ClarificationMessage>();
    public DbSet<PdfAttachment> PdfAttachments => Set<PdfAttachment>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfiguration(new DepartmentConfiguration());
        builder.ApplyConfiguration(new EmployeeConfiguration());
        builder.ApplyConfiguration(new FormCaseConfiguration());
        builder.ApplyConfiguration(new PdfAttachmentConfiguration());
    }
}