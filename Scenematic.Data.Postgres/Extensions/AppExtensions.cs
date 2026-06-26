using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Scenematic.Data.Postgres.Data;

namespace Scenematic.Data.Postgres.Extensions;

public static class AppExtensions
{
    public static async Task MigrateScenematicNpgsqlDataContext(this IServiceProvider services)
    {
        using var scope = services.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<ScenematicNpgsqlDataContext>();
        if (!await context.Database.CanConnectAsync())
        {
            throw new Exception(
                "Unable to connect to the Scenematic database. Please check your connection settings and ensure the database server is running."
            );
        }

        await context.Database.MigrateAsync();
    }
}