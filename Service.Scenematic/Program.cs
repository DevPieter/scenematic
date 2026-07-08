using MassTransit;
using Scalar.AspNetCore;
using Scenematic.Data.Models.DTOs.Cover;
using Scenematic.Data.Postgres.Extensions;
using Scenematic.Logic.Services;
using Scenematic.Queue;
using Scenematic.Queue.Contracts.GenerateCoverJobs;
using Scenematic.Queue.RabbitMQ.Extensions;
using Scenematic.Queue.RabbitMQ.Options;
using Service.Scenematic;
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
        x.AddConsumer<GenerateCoverJobCompletedConsumer>()
            .Endpoint(e => e.Name = ScenematicQueueNames.GenerateCover);
        x.AddConsumer<GenerateCoverJobFailedConsumer>()
            .Endpoint(e => e.Name = ScenematicQueueNames.GenerateCover);
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

app.MapPost("api/v1/generate-cover", async (
    CoverGeneratorJobService jobService,
    ISendEndpointProvider sendEndpointProvider,
    GenerateCoverRequest request
) =>
{
    var hash = CoverGeneratorJobService.GenerateCoverMetaHash(
        request.Title,
        request.Artist,
        request.CoverImageUrl,
        request.CoverVotes
    );

    var dto = new RegisterCoverJobDto(
        MetaHash: hash,
        AlbumTitle: request.Title,
        AlbumArtist: request.Artist,
        AlbumCoverImageUrl: request.CoverImageUrl,
        CoverVotes: request.CoverVotes
    );

    var existingJobResult = await jobService.GetByMetaHashAsync(hash, includeResults: true);
    if (existingJobResult.IsSuccess)
    {
        var existingJob = existingJobResult.Data;
        if (existingJob is null) return Results.Problem("Failed to retrieve existing job");

        var successResult = existingJob?.SuccessResult;
        var failureResult = existingJob?.FailureResult;

        return Results.Ok(new
            {
                existingJob.Id,
                existingJob.State,
                MetaHash = existingJob.CoverMetaHash,

                SuccessResult = successResult is null
                    ? null
                    : new
                    {
                        successResult.MainCoverPath,
                        successResult.DebounceCoverPath,
                        successResult.ProcessingCoverPath,
                        existingJob.CompletedAt
                    },

                FailureResult = failureResult is null
                    ? null
                    : new
                    {
                        failureResult.ErrorMessage,
                        existingJob.FailedAt
                    }
            }
        );
    }

    var job = await jobService.RegisterAsync(dto);
    var webhook = await jobService.RegisterWebhookAsync(job.Data.Id, request.CallbackUrl);

    await jobService.MarkQueuedAsync(job.Data.Id);

    var endpoint = await sendEndpointProvider.GetSendEndpoint(new Uri($"queue:{ScenematicQueueNames.GenerateCover}"));
    await endpoint.Send(new GenerateCoverJobRequested(job.Data.Id));

    return Results.Accepted(
        $"/generate-cover-jobs/{job.Data.Id}",
        new
        {
            job.Data.Id,
            job.Data.State,
            MetaHash = job.Data.CoverMetaHash,
            Callback = new
            {
                webhook.Data.Url,
                webhook.Data.Secret,
                Header = "X-Webhook-Secret"
            }
        }
    );
});

await app.Services.MigrateScenematicNpgsqlDataContext();
app.Run();