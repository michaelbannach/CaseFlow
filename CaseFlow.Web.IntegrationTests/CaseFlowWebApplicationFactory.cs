using System;
using System.Linq;
using CaseFlow.Infrastructure.Data;
using CaseFlow.Infrastructure.Seeding;
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
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // 1) Existing AppDbContext registration entfernen
            var dbContextDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (dbContextDescriptor is not null)
                services.Remove(dbContextDescriptor);

            // 2) Unique DB pro Test-Factory (keine Kollisionen, keine "AspNetRoles exists")
            //    Wir nehmen die Basis-ConnectionString aus der Config und ersetzen nur den DB-Namen.
            var sp0 = services.BuildServiceProvider();
            using var scope0 = sp0.CreateScope();
            var cfg = scope0.ServiceProvider.GetRequiredService<IConfiguration>();

            var baseConn = cfg.GetConnectionString("DefaultConnection")
                          ?? throw new InvalidOperationException("Missing connection string 'DefaultConnection'");

            var dbName = $"CaseFlow_Test_{Guid.NewGuid():N}";
            var conn = ReplaceDatabaseName(baseConn, dbName);

            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(conn));

            // 3) DB erstellen + Migration + Seed
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();

            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // Optional, aber robust:
            db.Database.EnsureDeleted();

            db.Database.Migrate();
            DevelopmentSeeder.SeedAsync(scope.ServiceProvider).GetAwaiter().GetResult();
        });
    }

    private static string ReplaceDatabaseName(string connectionString, string newDbName)
    {
        // Sehr einfache, robuste Ersetzung für typische SQLServer Connection Strings:
        // ...;Database=Foo;...  oder  ...;Initial Catalog=Foo;...
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
