using Scalar.AspNetCore;
using Scenematic.Data.Postgres.Extensions;
using Scenematic.Logic.Services;
using Scenematic.Queue;
using Scenematic.Queue.RabbitMQ.Extensions;
using Scenematic.Queue.RabbitMQ.Options;
using Service.Scenematic.Consumers;

var builder = WebApplication.CreateBuilder(args);

builder.Host.ConfigureHostOptions(options => { options.ShutdownTimeout = TimeSpan.FromSeconds(20); });

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var connectionString = builder.Configuration.GetConnectionString("Scenematic");
if (string.IsNullOrEmpty(connectionString))
    throw new InvalidOperationException("Scenematic Connection string is not configured.");

var rabbitMqOptions = builder.Configuration.GetSection("Scenematic:RabbitMq").Get<RabbitMqOptions>();
if (rabbitMqOptions is null)
    throw new InvalidOperationException("Scenematic RabbitMqOptions is not configured.");

builder.Services.UseScenematicNpgsqlContext(connectionString);
builder.Services.UseScenematicRabbitMq(
    rabbitMqOptions,
    x =>
    {
        x.AddConsumer<GenerateCoverJobResultConsumer>()
            .Endpoint(e => e.Name = ScenematicQueueNames.GenerateCoverResult);
    }
);

builder.Services.AddHttpClient<WebhookService>();
builder.Services.AddScoped<CoverGeneratorJobService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapControllers();

await app.Services.MigrateScenematicNpgsqlDataContext();
app.Run();