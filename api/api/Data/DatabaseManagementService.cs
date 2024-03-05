using Microsoft.EntityFrameworkCore;

namespace api.Data;
public static class DatabaseManagementService
{
    public static void MigrationInitialization(this IApplicationBuilder app)
    {
        using var serviceScope = app.ApplicationServices.CreateScope();
        var serviceDb = serviceScope.ServiceProvider.GetRequiredService<DataContext>();
        serviceDb.Database.Migrate();
    }
}
