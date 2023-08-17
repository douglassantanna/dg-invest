using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace function_api.Data;
public class DataContextFactory : IDesignTimeDbContextFactory<DataContext>
{
    public DataContext CreateDbContext(string[] args)
    {
        string connectionString = Environment.GetEnvironmentVariable("SqlConnectionString");

        var optionsBuilder = new DbContextOptionsBuilder<DataContext>();
        optionsBuilder.UseSqlServer("Server=tcp:dg-invest-server.database.windows.net,1433;Initial Catalog=dg-invest;Persist Security Info=False;User ID=dege;Password=6JrhbVM?;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;",
        sqlServerOptions => sqlServerOptions.EnableRetryOnFailure());
        return new DataContext(optionsBuilder.Options);
    }

}