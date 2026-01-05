using System;
using System.Linq;
using CaseFlow.Application.Interfaces;
using CaseFlow.Infrastructure.Data;
using CaseFlow.Infrastructure.Repositories;
using CaseFlow.Application.Services;
using CaseFlow.Infrastructure.Seeding;
using CaseFlow.Infrastructure.Storage;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CaseFlow.Web.IntegrationTests;

public sealed class CaseFlowWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Wichtig: Testing Environment (damit Program.cs z.B. HTTPS Redirect deaktivieren kann)
        builder.UseEnvironment("Testing");

        //  CRITICAL: JWT settings as ENV so Program.cs and AuthService use the SAME key/issuer/audience
        // (HS256 requires >= 32 bytes)
        Environment.SetEnvironmentVariable("Jwt__Key", "caseflow_test_jwt_key_32_chars_min_123456");
        Environment.SetEnvironmentVariable("Jwt__Issuer", "CaseFlow");
        Environment.SetEnvironmentVariable("Jwt__Audience", "CaseFlowClient");
        Environment.SetEnvironmentVariable("Jwt__ExpiresMinutes", "60");

        builder.ConfigureServices(services =>
        {
            // 1) Existing AppDbContext registration entfernen
            var dbContextDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));

            if (dbContextDescriptor is not null)
                services.Remove(dbContextDescriptor);

            // 2) Unique SQL DB pro Factory (keine Kollisionsprobleme)
            var sp0 = services.BuildServiceProvider();
            using var scope0 = sp0.CreateScope();
            var cfg = scope0.ServiceProvider.GetRequiredService<IConfiguration>();

            var baseConn = cfg.GetConnectionString("DefaultConnection")
                          ?? throw new InvalidOperationException("Missing connection string 'DefaultConnection'");

            var dbName = $"CaseFlow_Test_{Guid.NewGuid():N}";
            var conn = ReplaceDatabaseName(baseConn, dbName);

            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(conn));

            // 3) Explizit die Abhängigkeiten registrieren, die deine Services brauchen
            services.AddScoped<IEmployeeRepository, EmployeeRepository>();
            services.AddScoped<IClarificationMessageRepository, ClarificationMessageRepository>();
            services.AddScoped<IAttachmentStorage, LocalAttachmentStorage>();
            services.AddScoped<IPdfAttachmentRepository, PdfAttachmentRepository>();
            services.AddScoped<IAttachmentService, AttachmentService>();

            // 4) DB erstellen + Migration + Seed
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();

            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // Optional aber sauber: frische DB pro Factory
            db.Database.EnsureDeleted();

            db.Database.Migrate();
            DevelopmentSeeder.SeedAsync(scope.ServiceProvider).GetAwaiter().GetResult();
        });
    }

    private static string ReplaceDatabaseName(string connectionString, string newDbName)
    {
        var parts = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList();

        for (var i = 0; i < parts.Count; i++)
        {
            var p = parts[i].Trim();

            if (p.StartsWith("Database=", StringComparison.OrdinalIgnoreCase))
                parts[i] = $"Database={newDbName}";
            else if (p.StartsWith("Initial Catalog=", StringComparison.OrdinalIgnoreCase))
                parts[i] = $"Initial Catalog={newDbName}";
        }

        return string.Join(';', parts) + ";";
    }
}
