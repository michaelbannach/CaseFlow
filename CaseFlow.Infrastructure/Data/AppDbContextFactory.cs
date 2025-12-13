using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

using CaseFlow.Infrastructure.Data;

namespace CaseFlow.Infrastructure.Data;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var basePath = Path.Combine(Directory.GetCurrentDirectory(), @"../CaseFlow.Web");
        
        var config  = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();
        
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();

        optionsBuilder.UseSqlServer(
            config.GetConnectionString("DefaultConnection"),
            sql =>
            {
                sql.MigrationsAssembly(typeof(AppDbContextFactory).Assembly.FullName);
            });
        return new AppDbContext(optionsBuilder.Options);
    }
}