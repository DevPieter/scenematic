using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Scenematic.Data.Postgres.Data;
using Scenematic.Data.Postgres.Repositories;
using Scenematic.Data.Repositories;

namespace Scenematic.Data.Postgres.Extensions;

public static class BuilderExtensions
{
    public static void UseScenematicNpgsqlContext(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<ScenematicNpgsqlDataContext>(options => options.UseNpgsql(connectionString));

        services.AddScoped<ICoverJobRepository, CoverJobRepository>();
    }
}