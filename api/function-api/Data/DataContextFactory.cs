using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace function_api.Data;
public class DataContextFactory : IDesignTimeDbContextFactory<DataContext>
{
    public DataContext CreateDbContext(string[] args)
    {
        // string connectionString = Environment.GetEnvironmentVariable("ConnectionString") ?? throw new Exception("ConnectionString not found");

        var optionsBuilder = new DbContextOptionsBuilder<DataContext>();

        optionsBuilder.UseSqlServer("Server=localhost\\SQLEXPRESS;Database=dginvest;Trusted_Connection=True;Trust Server Certificate=true",
                                    sqlServerOptions => sqlServerOptions.EnableRetryOnFailure());

        return new DataContext(optionsBuilder.Options);
    }

}