using MassTransit;
using Scenematic.Bucket.MinIO.Extensions;
using Scenematic.Bucket.MinIO.Options;
using Scenematic.Data.Postgres.Extensions;
using Scenematic.Logic.Services;
using Scenematic.Queue;
using Scenematic.Queue.RabbitMQ.Extensions;
using Scenematic.Queue.RabbitMQ.Options;
using Scenematic.Worker.CoverGenerator.Consumers;
using Scenematic.Worker.CoverGenerator.Options;
using Scenematic.Worker.CoverGenerator.Processors;

var builder = WebApplication.CreateBuilder(args);
builder.Host.ConfigureHostOptions(options => { options.ShutdownTimeout = TimeSpan.FromSeconds(20); });

var connectionString = builder.Configuration.GetConnectionString("Scenematic");
if (string.IsNullOrEmpty(connectionString))
    throw new InvalidOperationException("Scenematic Connection string is not configured.");

var rabbitMqOptions = builder.Configuration.GetSection("Scenematic:RabbitMq").Get<RabbitMqOptions>();
if (rabbitMqOptions is null)
    throw new InvalidOperationException("Scenematic RabbitMqOptions is not configured.");

var minioOptions = builder.Configuration.GetSection("Scenematic:MinIO").Get<MinioBucketOptions>();
if (minioOptions is null)
    throw new InvalidOperationException("Scenematic MinIOOptions is not configured.");

builder.Services.Configure<BucketOptions>(builder.Configuration.GetSection("Scenematic:Bucket"));

builder.Services.UseScenematicNpgsqlContext(connectionString);
builder.Services.UseScenematicRabbitMq(
    rabbitMqOptions,
    x =>
    {
        x.AddConsumer<GenerateCoverRequestedConsumer>((context, configurator) =>
        {
            configurator.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(10)));
        }).Endpoint(e => e.Name = ScenematicQueueNames.GenerateCover);
    }
);
builder.Services.UseScenematicMinio(minioOptions);

builder.Services.AddHttpClient();
builder.Services.AddScoped<CoverImageProcessor>();
builder.Services.AddScoped<CoverGeneratorJobService>();

var app = builder.Build();
app.Run();

// const int Fps = 16;
// const int MainWidth = 1200;
// const int MainHeight = 400;
// const int VotesWidth = 1200;
// const int VotesHeight = 260;
//
// var client = new HttpClient();
// using var coverImage = await CoverImageProcessor.TryDownloadImage(client,
//     "https://content-images.p-cdn.com/images/c9/25/bc/5b/364a48b795558b2a06608ca4/_500W_500H.jpg");
// if (coverImage is null) throw new InvalidOperationException("Could not download the preview cover image.");
//
// var votes = new List<CoverVoteDto>
// {
//     new(
//         SongVoteLevel.Love,
//         "https://cdn.discordapp.com/avatars/427388881214898177/afe3572773a32e161b177e61eebf5abc.webp?size=128"
//     ),
//     new(
//         SongVoteLevel.Love,
//         "https://cdn.discordapp.com/avatars/360790692949393410/29ee2a707b057b873f87f478d97aa488.webp?size=128"
//     ),
//     new(
//         SongVoteLevel.Like,
//         "https://cdn.discordapp.com/avatars/724694495395840030/75946b85a5775694131c2c128979bbb7.webp?size=128"
//     ),
//     new(
//         SongVoteLevel.Neutral,
//         "https://cdn.discordapp.com/avatars/1117754055104671795/a6aa9b2547fe92dd482064c66a918518.webp?size=128"
//     ),
//     new(
//         SongVoteLevel.Dislike,
//         "https://cdn.discordapp.com/avatars/699322765114867764/937caec5a8c6eb66bcef18cddd4a2be5.webp?size=128"
//     )
// };
//
// var avatarImages = new Dictionary<string, SKImage>();
//
// foreach (var vote in votes)
// {
//     var avatarImage = await CoverImageProcessor.TryDownloadImage(client, vote.AvatarUrl);
//     if (avatarImage is not null) avatarImages[vote.AvatarUrl] = avatarImage;
// }
//
// using var mainRenderer = new Renderer(Fps, MainWidth, MainHeight);
// var mainCover = new CoverRenderer(
//     "Fake Shit",
//     "Damaxy",
//     coverImage
// );
//
// mainRenderer.Add(
//     new Scene(
//         "Main Cover",
//         0,
//         mainCover
//     ),
//     new Scene(
//         "Main Cover",
//         3,
//         mainCover
//     ),
//     new Scene(
//         "Main Cover",
//         2,
//         mainCover,
//         1
//     ).AddOverlay(new BrandingOverlay())
// );
//
// using var votesRenderer = new Renderer(Fps, VotesWidth, VotesHeight);
// var voteCover = new VotesRenderer(votes, coverImage, avatarImages);
// votesRenderer.Add(
//     new Scene("Votes", 0, voteCover),
//     new Scene("Votes", 3, voteCover)
// );
//
// mainRenderer.Initialize();
// votesRenderer.Initialize();
//
// var mainRender = mainRenderer.Render();
// var votesRender = votesRenderer.Render();
//
// var outputDirectory = @"C:\Users\devpi\Desktop\tijdperk";
// await File.WriteAllBytesAsync(Path.Combine(outputDirectory, "cover.webp"), mainRender);
// await File.WriteAllBytesAsync(Path.Combine(outputDirectory, "votes.webp"), votesRender);
//
// foreach (var avatarImage in avatarImages.Values) avatarImage.Dispose();