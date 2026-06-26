using MassTransit;
using Scenematic.Bucket.MinIO.Extensions;
using Scenematic.Bucket.MinIO.Options;
using Scenematic.Bucket.Extensions;
using Scenematic.Data.Postgres.Extensions;
using Scenematic.Logic.Services;
using Scenematic.Queue;
using Scenematic.Queue.RabbitMQ.Extensions;
using Scenematic.Queue.RabbitMQ.Options;
using Scenematic.Worker.CoverGenerator.Consumers;
using Scenematic.Worker.CoverGenerator.Processors;

var builder = WebApplication.CreateBuilder(args);
builder.Host.ConfigureHostOptions(options => { options.ShutdownTimeout = TimeSpan.FromSeconds(20); });

builder.RegisterBucketOptions("Scenematic:Bucket");

var connectionString = builder.Configuration.GetConnectionString("Scenematic");
if (string.IsNullOrEmpty(connectionString))
    throw new InvalidOperationException("Scenematic Connection string is not configured.");

var rabbitMqOptions = builder.Configuration.GetSection("Scenematic:RabbitMq").Get<RabbitMqOptions>();
if (rabbitMqOptions is null)
    throw new InvalidOperationException("Scenematic RabbitMqOptions is not configured.");

var minioOptions = builder.Configuration.GetSection("Scenematic:MinIO").Get<MinioBucketOptions>();
if (minioOptions is null)
    throw new InvalidOperationException("Scenematic MinIOOptions is not configured.");

builder.Services.UseScenematicNpgsqlContext(connectionString);
builder.Services.UseScenematicRabbitMq(
    rabbitMqOptions,
    x =>
    {
        x.AddConsumer<GenerateCoverJobRequestedConsumer>((context, configurator) =>
        {
            configurator.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(10)));
        }).Endpoint(e => e.Name = ScenematicQueueNames.GenerateCover);
    }
);
builder.Services.UseScenematicMinIO(minioOptions);

builder.Services.AddHttpClient();
builder.Services.AddScoped<CoverProcessor>();
builder.Services.AddScoped<CoverGeneratorJobService>();

var app = builder.Build();
app.Run();