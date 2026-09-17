using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;

namespace Gnoss.Web.OAuth.Infrastructure.Persistence
{
    public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            var provider = config.GetValue<string>("DatabaseProvider", "PostgreSQL")!;
            var connectionString = config.GetConnectionString("DefaultConnection")!;

            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();

            if (provider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
                optionsBuilder.UseSqlServer(connectionString);
            else if (provider.Equals("Oracle", StringComparison.OrdinalIgnoreCase))
                // Add Oracle.EntityFrameworkCore (EF Core 10 compatible) to the .csproj, then replace with:
                // optionsBuilder.UseOracle(connectionString);
                throw new NotSupportedException(
                    "Oracle requires Oracle.EntityFrameworkCore compatible with EF Core 10. See CLAUDE.md.");
            else
                optionsBuilder.UseNpgsql(connectionString);

            return new AppDbContext(optionsBuilder.Options);
        }
    }
}
