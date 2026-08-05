using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace api.Data;

public class DataContextFactory : IDesignTimeDbContextFactory<DataContext>
{
    public DataContext CreateDbContext(string[] args)
    {
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = config.GetConnectionString("DefaultConnection")
            ?? config["ConnectionStrings__DefaultConnection"]
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is missing.");

        var options = new DbContextOptionsBuilder<DataContext>()
            .UseSqlServer(connectionString, x => x.EnableRetryOnFailure())
            .Options;

        return new DataContext(options);
    }
}
