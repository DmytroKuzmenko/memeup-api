using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Memeup.Api.Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<MemeupDbContext>
{
    public MemeupDbContext CreateDbContext(string[] args)
    {
        var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.{env}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var cs = config.GetConnectionString("DefaultConnection")
                 ?? "Host=localhost;Port=5432;Database=memeup;Username=postgres;Password=postgres";

        var options = new DbContextOptionsBuilder<MemeupDbContext>()
            .UseNpgsql(cs, npg => npg.EnableRetryOnFailure())
            .Options;

        return new MemeupDbContext(options);
    }
}
