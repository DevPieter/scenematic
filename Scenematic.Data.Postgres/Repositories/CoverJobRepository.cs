using Microsoft.EntityFrameworkCore;
using Scenematic.Common.Enums;
using Scenematic.Data.Models.DTOs.Cover;
using Scenematic.Data.Models.DTOs.Webhooks;
using Scenematic.Data.Models.Entities.Cover;
using Scenematic.Data.Models.Entities.Webhooks;
using Scenematic.Data.Postgres.Data;
using Scenematic.Data.Repositories;

namespace Scenematic.Data.Postgres.Repositories;

public class CoverJobRepository(ScenematicNpgsqlDataContext context) : ICoverJobRepository
{
    public async Task<CoverJob> RegisterAsync(RegisterCoverJobDto dto)
    {
        var job = new CoverJob(coverMetaHash: dto.MetaHash);
        await context.CoverJobs.AddAsync(job);
        await context.SaveChangesAsync();

        // var coverVotes = dto.CoverVotes.ConvertAll(cv => new CoverVoteMeta(
        //     voteLevel: cv.VoteLevel,
        //     avatarUrl: cv.AvatarUrl
        // ));

        var meta = new CoverMeta(
            albumTitle: dto.AlbumTitle,
            albumArtist: dto.AlbumArtist,
            albumCoverImageUrl: dto.AlbumCoverImageUrl
            // coverVotes: coverVotes
        );

        await context.CoverMetas.AddAsync(meta);
        job.CoverMeta = meta;

        await context.SaveChangesAsync();
        return job;
    }

    public async Task<CoverJobWebhook?> RegisterWebhookAsync(Guid jobId, RegisterWebhookDto dto)
    {
        var job = await context.CoverJobs.FirstOrDefaultAsync(j => j.Id == jobId);
        if (job is null) return null;

        var webhook = new Webhook(
            url: dto.Url,
            secret: dto.Secret
        );

        var jobWebhook = new CoverJobWebhook(
            jobId: jobId,
            webhookId: webhook.Id
        );

        await context.Webhooks.AddAsync(webhook);
        await context.CoverJobWebhooks.AddAsync(jobWebhook);
        await context.SaveChangesAsync();

        return jobWebhook;
    }

    public async Task<CoverJob?> GetByIdAsync(
        Guid id,
        bool includeMetadata = false,
        bool includeResults = false,
        bool includeWebhooks = false
    )
    {
        var query = context.CoverJobs.AsNoTracking();

        if (includeMetadata)
        {
            query = query.Include(j => j.CoverMeta);
            // .ThenInclude(m => m.CoverVotes);
        }

        if (includeResults)
        {
            query = query.Include(j => j.SuccessResult)
                .Include(j => j.FailureResult);
        }

        if (includeWebhooks)
        {
            query = query.Include(j => j.Webhooks);
        }

        return await query.FirstOrDefaultAsync(j => j.Id == id);
    }

    public async Task<bool> DoesMetaHashExistAsync(string metaHash)
        => await context.CoverJobs.AsNoTracking()
            .AnyAsync(j => j.CoverMetaHash == metaHash);

    public async Task<CoverJob?> GetByMetaHashAsync(
        string metaHash,
        bool includeMetadata = false,
        bool includeResults = false,
        bool includeWebhooks = false
    )
    {
        var query = context.CoverJobs.AsNoTracking();

        if (includeMetadata)
        {
            query = query.Include(j => j.CoverMeta);
            // .ThenInclude(m => m.CoverVotes);
        }

        if (includeResults)
        {
            query = query.Include(j => j.SuccessResult)
                .Include(j => j.FailureResult);
        }

        if (includeWebhooks)
        {
            query = query.Include(j => j.Webhooks);
        }

        return await query.FirstOrDefaultAsync(j => j.CoverMetaHash == metaHash);
    }

    public async Task<bool> DoesWebhookExistAsync(Guid jobId, string url)
        => await context.CoverJobWebhooks.AsNoTracking()
            .Where(jw => jw.JobId == jobId)
            .Include(jw => jw.Webhook)
            .AnyAsync(jw => jw.Webhook.Url == url);

    public async Task<List<Webhook>> GetWebhooksAsync(Guid jobId)
        => await context.CoverJobWebhooks.AsNoTracking()
            .Where(jw => jw.JobId == jobId)
            .Include(jw => jw.Webhook)
            .Select(jw => jw.Webhook)
            .ToListAsync();

    public Task MarkQueuedIfPendingAsync(Guid id)
    {
        var queuedAt = DateTime.UtcNow;
        return context.CoverJobs
            .Where(job => job.Id == id && job.State == GenerateCoverJobState.Pending)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(job => job.State, GenerateCoverJobState.Queued)
                .SetProperty(job => job.QueuedAt, job => job.QueuedAt ?? queuedAt));
    }

    public async Task<bool> UpdateAsync(Guid id, UpdateCoverJobDto jobDto)
    {
        var job = await context.CoverJobs.FirstOrDefaultAsync(j => j.Id == id);
        if (job is null) return false;

        job.State = jobDto.State;

        job.QueuedAt ??= jobDto.QueuedAt;
        job.StartedAt ??= jobDto.StartedAt;
        job.CompletedAt ??= jobDto.CompletedAt;
        job.FailedAt ??= jobDto.FailedAt;

        await context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> HasSuccessAsync(Guid id)
    {
        var result = await context.CoverJobSuccessResults.AsNoTracking()
            .FirstOrDefaultAsync(r => r.JobId == id);

        return result is not null;
    }

    public async Task<CoverJobSuccess?> SetSuccessAsync(Guid id, SetCoverJobSuccessDto dto)
    {
        var job = await context.CoverJobs.FirstOrDefaultAsync(j => j.Id == id);
        if (job is null) return null;

        var result = new CoverJobSuccess(
            jobId: id,
            mainCoverPath: dto.MainCoverPath
            // debounceCoverPath: dto.DebounceCoverPath,
            // processingCoverPath: dto.ProcessingCoverPath
        );

        job.SuccessResult = result;

        await context.SaveChangesAsync();
        return result;
    }

    public async Task<bool> HasFailureAsync(Guid id)
    {
        var result = await context.CoverJobFailureResults.AsNoTracking()
            .FirstOrDefaultAsync(r => r.JobId == id);

        return result is not null;
    }

    public async Task<CoverJobFailure?> SetFailureAsync(Guid id, SetCoverJobFailureDto dto)
    {
        var job = await context.CoverJobs.FirstOrDefaultAsync(j => j.Id == id);
        if (job is null) return null;

        var result = new CoverJobFailure(
            jobId: id,
            failureReason: dto.FailureReason,
            errorMessage: dto.ErrorMessage
        );

        job.FailureResult = result;

        await context.SaveChangesAsync();
        return result;
    }

    public async Task<bool> DeleteMetaAsync(Guid id)
    {
        var job = await context.CoverJobs
            .Include(j => j.CoverMeta)
            // .ThenInclude(m => m.CoverVotes)
            .FirstOrDefaultAsync(j => j.Id == id);
        if (job is null) return false;

        if (job.CoverMeta is not null)
        {
            // context.CoverVoteMetas.RemoveRange(job.CoverMeta.CoverVotes);
            context.CoverMetas.Remove(job.CoverMeta);
        }

        job.CoverMetaId = null;

        await context.SaveChangesAsync();
        return true;
    }
}