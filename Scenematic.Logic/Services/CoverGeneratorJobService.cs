using System.Security.Cryptography;
using System.Text;
using MassTransit;
using Microsoft.Extensions.Logging;
using Scenematic.Common.Enums;
using Scenematic.Common.Models.Common;
using Scenematic.Data.Models.DTOs.Cover;
using Scenematic.Data.Models.DTOs.Webhooks;
using Scenematic.Data.Models.Entities.Cover;
using Scenematic.Data.Models.Entities.Webhooks;
using Scenematic.Data.Repositories;
using Scenematic.Logic.Models.Arguments.CoverJobArgs;
using Scenematic.Queue;
using Scenematic.Queue.Contracts.GenerateCoverJobs;

namespace Scenematic.Logic.Services;

public class CoverGeneratorJobService(
    ICoverJobRepository repository,
    ISendEndpointProvider sendEndpointProvider,
    ILogger<CoverGeneratorJobService> logger
)
{
    private static string GenerateCoverMetaHash(
        string albumTitle,
        string albumArtist,
        string albumCoverImageUrl
        // List<CoverVoteDto> coverVotes
    )
    {
        // var voteStr = string.Join("|",
        //     coverVotes.OrderBy(cv => cv.AvatarUrl).Select(cv => $"{cv.VoteLevel}:{cv.AvatarUrl}")
        // );

        var str = $"{albumTitle}|{albumArtist}|{albumCoverImageUrl}";
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(str));
        return Convert.ToBase64String(hashBytes);
    }

    // TODO - Move to crypto utils/service
    private static string GenerateCryptoRandomHash(int length)
    {
        var bytes = new byte[length];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }

    public async Task<ResultObject<RegisterCoverJobResult>> RegisterOrGetAsync(RegisterCoverJobArgs args)
    {
        var metaHash = GenerateCoverMetaHash(
            args.Title,
            args.Artist,
            args.CoverImageUrl
            // args.CoverVotes
        );

        var isWebhookProvided = !string.IsNullOrWhiteSpace(args.CallbackUrl); // TODO - Add more url validation
        var doesJobExist = await repository.DoesMetaHashExistAsync(metaHash);

        if (!isWebhookProvided && !doesJobExist)
        {
            return ResultObject<RegisterCoverJobResult>.BadRequest(
                "Callback URL is required for new cover generation jobs"
            );
        }

        CoverJob job;

        try
        {
            job = doesJobExist
                ? await repository.GetByMetaHashAsync(
                    metaHash: metaHash,
                    includeMetadata: true,
                    includeResults: true,
                    includeWebhooks: true
                ) ?? throw new Exception("Failed to retrieve existing job")
                : await repository.RegisterAsync(new RegisterCoverJobDto(
                    MetaHash: metaHash,
                    AlbumTitle: args.Title,
                    AlbumArtist: args.Artist,
                    AlbumCoverImageUrl: args.CoverImageUrl
                    // CoverVotes: args.CoverVotes
                ));
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to register or get cover generation job"
            );
            return ResultObject<RegisterCoverJobResult>.InternalError(
                "Failed to register or get cover generation job"
            );
        }

        Webhook? webhook = null;

        if (isWebhookProvided)
        {
            var webhookResult = await RegisterWebhookAsync(job.Id, args.CallbackUrl!);
            if (webhookResult.IsError)
            {
                return ResultObject<RegisterCoverJobResult>.InternalError(
                    "Failed to register webhook for cover generation job"
                );
            }

            webhook = webhookResult.Data;
        }

        if (doesJobExist)
        {
            return ResultObject<RegisterCoverJobResult>.Success(new RegisterCoverJobResult(
                Job: job,
                Webhook: webhook
            ));
        }

        try
        {
            var endpoint = await sendEndpointProvider.GetSendEndpoint(
                new Uri($"queue:{ScenematicQueueNames.GenerateCover}")
            );

            await endpoint.Send(new GenerateCoverRequested(job.Id));

            var markQueuedResult = await MarkQueuedAsync(job.Id);
            if (markQueuedResult.IsSuccess)
            {
                return ResultObject<RegisterCoverJobResult>.Success(new RegisterCoverJobResult(
                    Job: job,
                    Webhook: webhook
                ));
            }

            logger.LogError(
                "Failed to mark cover generation job {JobId} as queued after sending GenerateCoverRequested message",
                job.Id
            );
            return ResultObject<RegisterCoverJobResult>.InternalError(
                "Failed to mark cover generation job as queued after sending GenerateCoverRequested message"
            );
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to send GenerateCoverRequested message for cover generation job {JobId}",
                job.Id
            );
            return ResultObject<RegisterCoverJobResult>.InternalError(
                "Failed to send GenerateCoverRequested message for cover generation job"
            );
        }
    }

    private async Task<ResultObject<Webhook?>> RegisterWebhookAsync(
        Guid jobId,
        string callbackUrl
    )
    {
        // If a webhook with the same URL already exists for this job, we don't need to register a new one.
        // We return null for the webhook since the caller should already know the secret for the hook.
        var doesWebhookExist = await repository.DoesWebhookExistAsync(jobId, callbackUrl);
        if (doesWebhookExist) return ResultObject<Webhook?>.Success();

        try
        {
            var secret = GenerateCryptoRandomHash(32);
            if (string.IsNullOrWhiteSpace(secret) || secret.Length < 32)
            {
                logger.LogError(
                    "Failed to generate a valid secret for webhook registration for cover generation job {JobId}",
                    jobId
                );
                return ResultObject<Webhook?>.InternalError(
                    "Failed to generate a valid secret for webhook registration"
                );
            }

            var webhookDto = new RegisterWebhookDto(
                Url: callbackUrl,
                Secret: secret
            );

            var coverJobWebhook = await repository.RegisterWebhookAsync(jobId, webhookDto);
            if (coverJobWebhook is not null)
            {
                return ResultObject<Webhook?>.Success(coverJobWebhook.Webhook);
            }

            logger.LogError(
                "Failed to register webhook for cover generation job {JobId}",
                jobId
            );
            return ResultObject<Webhook?>.InternalError(
                "Failed to register webhook for cover generation job"
            );
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to register webhook for cover generation job {JobId}",
                jobId
            );
            return ResultObject<Webhook?>.InternalError(
                "Failed to register webhook for cover generation job"
            );
        }
    }

    // public async Task<ResultObject<Webhook>> RegisterWebhookAsync(
    //     Guid jobId,
    //     string webhookUrl
    // )
    // {
    //     try
    //     {
    //         var secret = GenerateCryptoRandomHash(32);
    //         var webhookDto = new RegisterWebhookDto(
    //             Url: webhookUrl,
    //             Secret: secret
    //         );
    //
    //         var coverJobWebhook = await repository.RegisterWebhookAsync(jobId, webhookDto);
    //         return coverJobWebhook is null
    //             ? ResultObject<Webhook>.NotFound("Job not found")
    //             : ResultObject<Webhook>.Success(coverJobWebhook.Webhook);
    //     }
    //     catch (Exception ex)
    //     {
    //         logger.LogError(ex, "Failed to register webhook for cover generation job {JobId}", jobId);
    //         return ResultObject<Webhook>.InternalError("Failed to register webhook for cover generation job");
    //     }
    // }

    public async Task<ResultObject<CoverJob>> GetByIdAsync(
        Guid jobId,
        bool includeMetadata = false,
        bool includeResults = false
    )
    {
        try
        {
            var job = await repository.GetByIdAsync(
                id: jobId,
                includeMetadata: includeMetadata,
                includeResults: includeResults
            );

            return job is null
                ? ResultObject<CoverJob>.NotFound("Job not found")
                : ResultObject<CoverJob>.Success(job);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get cover generation job by id {JobId}", jobId);
            return ResultObject<CoverJob>.InternalError("Failed to get cover generation job");
        }
    }

    public async Task<ResultObject<CoverJob>> GetByMetaHashAsync(
        string metaHash,
        bool includeMetadata = false,
        bool includeResults = false
    )
    {
        try
        {
            var job = await repository.GetByMetaHashAsync(
                metaHash: metaHash,
                includeMetadata: includeMetadata,
                includeResults: includeResults
            );

            return job is null
                ? ResultObject<CoverJob>.NotFound("Job not found")
                : ResultObject<CoverJob>.Success(job);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get cover generation job by meta hash {MetaHash}", metaHash);
            return ResultObject<CoverJob>.InternalError("Failed to get cover generation job");
        }
    }

    private async Task<Result> MarkQueuedAsync(Guid jobId)
    {
        try
        {
            var job = await repository.GetByIdAsync(jobId);
            if (job is null) return Result.NotFound("Job not found");

            var dto = ToUpdateDto(job);
            dto.State = GenerateCoverJobState.Queued;

            dto.QueuedAt = DateTime.UtcNow;
            if (dto.StartedAt is not null) dto.StartedAt = null;
            if (dto.CompletedAt is not null) dto.CompletedAt = null;
            if (dto.FailedAt is not null) dto.FailedAt = null;

            var success = await repository.UpdateAsync(jobId, dto);
            return success
                ? Result.Success()
                : Result.InternalError("Failed to mark job as queued");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to mark job as queued");
            return Result.InternalError("Failed to mark job as queued");
        }
    }

    public async Task<Result> MarkStartedAsync(Guid jobId)
    {
        try
        {
            var job = await repository.GetByIdAsync(jobId);
            if (job is null) return Result.NotFound("Job not found");

            var dto = ToUpdateDto(job);
            dto.State = GenerateCoverJobState.Started;

            dto.StartedAt = DateTime.UtcNow;
            dto.QueuedAt ??= DateTime.UtcNow;
            if (dto.CompletedAt is not null) dto.CompletedAt = null;
            if (dto.FailedAt is not null) dto.FailedAt = null;

            var success = await repository.UpdateAsync(jobId, dto);
            return success
                ? Result.Success()
                : Result.InternalError("Failed to mark job as started");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to mark job as started");
            return Result.InternalError("Failed to mark job as started");
        }
    }

    public async Task<Result> MarkCompletedAsync(Guid jobId,
        // TODO - Get as args object, not params
        string mainCoverPath
        // string debounceCoverPath,
        // string processingCoverPath
    )
    {
        try
        {
            var job = await repository.GetByIdAsync(jobId);
            if (job is null) return Result.NotFound("Job not found");

            var result = await repository.SetSuccessAsync(jobId, new SetCoverJobSuccessDto(
                MainCoverPath: mainCoverPath
                // DebounceCoverPath: debounceCoverPath,
                // ProcessingCoverPath: processingCoverPath
            ));

            if (result is null)
            {
                logger.LogError("Failed to set success result for job id {JobId}", jobId);
                return Result.InternalError("Failed to set success result for job");
            }

            var dto = ToUpdateDto(job);
            dto.State = GenerateCoverJobState.Completed;

            dto.CompletedAt = DateTime.UtcNow;
            dto.QueuedAt ??= DateTime.UtcNow;
            dto.StartedAt ??= DateTime.UtcNow;
            if (dto.FailedAt is not null) dto.FailedAt = null;

            var success = await repository.UpdateAsync(jobId, dto);
            if (!success)
            {
                logger.LogError("Failed to mark job as completed for job id {JobId}", jobId);
                return Result.InternalError("Failed to mark job as completed");
            }

            var deleteSuccess = await repository.DeleteMetaAsync(jobId);
            if (!deleteSuccess)
            {
                logger.LogError("Failed to delete meta for completed job id {JobId}", jobId);
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to mark job as completed");
            return Result.InternalError("Failed to mark job as completed");
        }
    }

    public async Task<Result> MarkFailedAsync(
        Guid jobId,
        GenerateCoverJobFailureReason reason,
        string errorMessage
    )
    {
        try
        {
            var job = await repository.GetByIdAsync(jobId);
            if (job is null) return Result.NotFound("Job not found");

            var result = await repository.SetFailureAsync(jobId, new SetCoverJobFailureDto(
                FailureReason: reason,
                ErrorMessage: errorMessage
            ));

            if (result is null)
            {
                logger.LogError(
                    "Failed to set failure result for job id {JobId}, continuing to mark job as failed",
                    jobId
                );
            }

            var dto = ToUpdateDto(job);
            dto.State = GenerateCoverJobState.Failed;

            dto.FailedAt = DateTime.UtcNow;
            dto.QueuedAt ??= DateTime.UtcNow;
            dto.StartedAt ??= DateTime.UtcNow;
            if (dto.CompletedAt is not null) dto.CompletedAt = null;

            var success = await repository.UpdateAsync(jobId, dto);

            return success
                ? Result.Success()
                : Result.InternalError("Failed to mark job as failed");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to mark job as failed");
            return Result.InternalError("Failed to mark job as failed");
        }
    }

    private static UpdateCoverJobDto ToUpdateDto(CoverJob job) => new(
        state: job.State,
        queuedAt: job.QueuedAt,
        startedAt: job.StartedAt,
        completedAt: job.CompletedAt,
        failedAt: job.FailedAt
    );
}