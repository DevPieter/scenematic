using MassTransit;
using Scenematic.Common.Enums;
using Scenematic.Common.Enums.Common;
using Scenematic.Data.Models.DTOs.Cover;
using Scenematic.Logic.Services;
using Scenematic.Worker.CoverGenerator.Models.Covers;
using Scenematic.Worker.CoverGenerator.Processors;
using Scenematic.Queue.Contracts.GenerateCoverJobs;
using Scenematic.Worker.CoverGenerator.Enums;

namespace Scenematic.Worker.CoverGenerator.Consumers;

public class GenerateCoverRequestedConsumer(
    IServiceScopeFactory scopeFactory,
    ILogger<GenerateCoverRequestedConsumer> logger
) : IConsumer<GenerateCoverRequested>
{
    public async Task Consume(ConsumeContext<GenerateCoverRequested> context)
    {
        using var scope = scopeFactory.CreateScope();
        var jobService = scope.ServiceProvider.GetRequiredService<CoverGeneratorJobService>();
        var processor = scope.ServiceProvider.GetRequiredService<CoverImageProcessor>();

        var jobId = context.Message.JobId;

        try
        {
            var result = await ProcessAsync(
                jobId,
                jobService,
                processor,
                context.CancellationToken
            );

            switch (result)
            {
                case ProcessJobOutcome.Completed:
                    await context.Publish(new GenerateCoverResult(
                        JobId: jobId,
                        Success: true
                    ));
                    break;

                case ProcessJobOutcome.Failed or ProcessJobOutcome.NotFound:
                    await context.Publish(new GenerateCoverResult(
                        JobId: jobId,
                        Success: false
                    ));
                    break;

                case ProcessJobOutcome.Ignored:
                    break;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected failure while processing job {JobId}", jobId);
            throw;
        }
    }

    private async Task<ProcessJobOutcome> ProcessAsync(
        Guid jobId,
        CoverGeneratorJobService jobService,
        CoverImageProcessor imageProcessor,
        CancellationToken cancellationToken
    )
    {
        var jobResult = await jobService.GetByIdAsync(
            jobId: jobId,
            includeMetadata: true
        );

        if (jobResult.Type == ResultType.NotFound)
        {
            logger.LogWarning(
                "Cover generation job {JobId} could not be found: {Message}",
                jobId,
                jobResult.Message
            );

            return ProcessJobOutcome.NotFound;
        }

        if (jobResult.IsError)
        {
            logger.LogError(
                "Cover generation job {JobId} returned with error: {Message}",
                jobId,
                jobResult.Message
            );

            throw new InvalidOperationException(
                jobResult.Message ?? $"Failed to retrieve cover generation job {jobId}"
            );
        }

        var job = jobResult.Data;
        if (job?.CoverMeta is null)
        {
            logger.LogError(
                "Cover generation job {JobId} returned success but data is null",
                jobId
            );

            await MarkFailedAsync(
                jobId,
                jobService,
                GenerateCoverJobFailureReason.InternalError,
                "Cover generation job returned success but data is null"
            );

            return ProcessJobOutcome.Failed;
        }

        if (job.State is GenerateCoverJobState.Completed or GenerateCoverJobState.Failed)
        {
            logger.LogInformation(
                "Cover generation job {JobId} is already in terminal state {State}",
                jobId,
                job.State
            );

            return ProcessJobOutcome.Ignored;
        }

        logger.LogInformation("Processing cover generation job {JobId}", jobId);
        await MarkStartedAsync(jobId, jobService);

        var result = await imageProcessor.ProcessAsync(new ProcessCoverArgs(
                JobId: jobId,
                MetaHash: job.CoverMetaHash,
                AlbumTitle: job.CoverMeta.AlbumTitle,
                AlbumArtist: job.CoverMeta.AlbumArtist,
                AlbumCoverImageUrl: job.CoverMeta.AlbumCoverImageUrl
                // CoverVotes: job.CoverMeta.CoverVotes.Select(cv => new CoverVoteDto(
                //     cv.VoteLevel,
                //     cv.AvatarUrl
                // )).ToList()
            ),
            cancellationToken
        );

        if (result.IsError)
        {
            logger.LogError(
                "Cover generation job {JobId} failed: {Message}",
                jobId,
                result.Message
            );

            await MarkFailedAsync(
                jobId,
                jobService,
                result.FailureReason,
                result.Message ?? "Unknown error"
            );

            return ProcessJobOutcome.Failed;
        }

        logger.LogInformation("Cover generation job {JobId} completed successfully", jobId);

        if (
            string.IsNullOrEmpty(result.MainCoverPath)
            // || string.IsNullOrEmpty(result.DebounceCoverPath)
            // || string.IsNullOrEmpty(result.ProcessingCoverPath)
        )
        {
            logger.LogError(
                "Cover generation job {JobId} completed successfully but result paths are invalid",
                jobId
            );

            await MarkFailedAsync(
                jobId,
                jobService,
                GenerateCoverJobFailureReason.InternalError,
                "Cover processor returned invalid result paths"
            );

            return ProcessJobOutcome.Failed;
        }

        await MarkSuccessAsync(
            jobId,
            jobService,
            result.MainCoverPath
            // result.DebounceCoverPath,
            // result.ProcessingCoverPath
        );

        return ProcessJobOutcome.Completed;
    }

    private async Task MarkStartedAsync(
        Guid jobId,
        CoverGeneratorJobService jobService
    )
    {
        var markResult = await jobService.MarkStartedAsync(
            jobId: jobId
        );

        if (markResult.IsSuccess) return;

        logger.LogError(
            "Failed to mark job {JobId} as started: {Message}",
            jobId,
            markResult.Message
        );

        throw new InvalidOperationException(
            markResult.Message ?? $"Failed to mark job {jobId} as started"
        );
    }

    private async Task MarkFailedAsync(
        Guid jobId,
        CoverGeneratorJobService jobService,
        GenerateCoverJobFailureReason reason,
        string message
    )
    {
        var markResult = await jobService.MarkFailedAsync(
            jobId: jobId,
            reason: reason,
            errorMessage: message
        );

        if (markResult.IsSuccess) return;

        logger.LogError(
            "Failed to mark job {JobId} as failed: {Message}",
            jobId,
            markResult.Message
        );

        throw new InvalidOperationException(
            markResult.Message ?? $"Failed to mark job {jobId} as failed"
        );
    }

    private async Task MarkSuccessAsync(
        Guid jobId,
        CoverGeneratorJobService jobService,
        string mainCoverPath
        // string debounceCoverPath,
        // string processingCoverPath
    )
    {
        var markResult = await jobService.MarkCompletedAsync(
            jobId: jobId,
            mainCoverPath: mainCoverPath
            // debounceCoverPath: debounceCoverPath,
            // processingCoverPath: processingCoverPath
        );

        if (markResult.IsSuccess) return;

        logger.LogError(
            "Failed to mark job {JobId} as completed: {Message}",
            jobId,
            markResult.Message
        );

        throw new InvalidOperationException(
            markResult.Message ?? $"Failed to mark job {jobId} as completed"
        );
    }
}