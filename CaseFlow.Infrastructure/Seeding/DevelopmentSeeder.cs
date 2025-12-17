using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using CaseFlow.Infrastructure.Data;
using CaseFlow.Infrastructure.Models;
using CaseFlow.Domain.Models;
using CaseFlow.Domain.Enums;

namespace CaseFlow.Infrastructure.Seeding;

public static class DevelopmentSeeder
{
    private static bool _initialized;
    private static readonly object _lock = new();

    public static async Task SeedAsync(IServiceProvider services)
    {
        if (_initialized) return;
        lock (_lock)
        {
            if (_initialized) return;
            _initialized = true;
        }

        using var scope = services.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

   
        if (!await db.Departments.AnyAsync())
        {
            var departments = new List<Department>
            {
                new() { Name = "Allgemein" },
                new() { Name = "Leistungen" },
                new() { Name = "Kosten" },
                new() { Name = "Organisation" }
            };

            await db.Departments.AddRangeAsync(departments);
            await db.SaveChangesAsync();
        }

        var departmentId = await db.Departments
            .OrderBy(d => d.Id)
            .Select(d => d.Id)
            .FirstAsync();

      
        const string email = "seed_admin@caseflow.local";
        const string password = "Test123!";

        var identityUser =
            await userManager.FindByEmailAsync(email)
            ?? await userManager.FindByNameAsync(email);

        if (identityUser == null)
        {
            identityUser = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(identityUser, password);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new Exception($"Seeding IdentityUser failed: {errors}");
            }
        }

        
        //Employee (FK: FormCases.CreateByEmployeeId)
        
        var employee = await db.Employees
            .FirstOrDefaultAsync(e => e.ApplicationUserId == identityUser.Id);

        if (employee == null)
        {
            employee = new Employee
            {
                ApplicationUserId = identityUser.Id,
                Name = "Seed Admin",
                Role = UserRole.Sachbearbeiter,
                DepartmentId = departmentId
            };

            db.Employees.Add(employee);
            await db.SaveChangesAsync();
        }
        
        if (!await db.FormCases.AnyAsync())
        {
            db.FormCases.Add(new FormCase
            {
                FormType = FormType.Leistungsantrag,
                Status = ProcessingStatus.Neu,

                DepartmentId = departmentId,
                CreateByEmployeeId = employee.Id,

                ApplicantName = "Max Mustermann",
                ApplicantStreet = "Musterstraße 1",
                ApplicantZip = 12345,
                ApplicantCity = "Musterstadt",
                ApplicantPhone = "0000000000",
                ApplicantEmail = "max@caseflow.local",

                Subject = "Seed Testfall",
                Notes = "Automatisch erzeugter Testfall"
            });

            await db.SaveChangesAsync();
        }
    }
}
