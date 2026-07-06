using Microsoft.EntityFrameworkCore;
using Scenematic.Data.Models.DTOs.Cover;
using Scenematic.Data.Models.Entities.Cover;
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

        try
        {
            var coverVotes = dto.CoverVotes.ConvertAll(cv => new CoverVoteMeta(
                voteLevel: cv.VoteLevel,
                avatarUrl: cv.AvatarUrl
            ));

            var meta = new CoverMeta(
                albumTitle: dto.AlbumTitle,
                albumArtist: dto.AlbumArtist,
                albumCoverImageUrl: dto.AlbumCoverImageUrl,
                coverVotes: coverVotes
            );

            job.CoverMeta = meta;
            await context.SaveChangesAsync();
        }
        catch
        {
            context.CoverJobs.Remove(job);
            await context.SaveChangesAsync();
            throw;
        }

        return job;
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
            query = query.Include(j => j.CoverMeta)
                .ThenInclude(m => m.CoverVotes);
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
            query = query.Include(j => j.CoverMeta)
                .ThenInclude(m => m.CoverVotes);
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
            mainCoverPath: dto.MainCoverPath,
            debounceCoverPath: dto.DebounceCoverPath,
            processingCoverPath: dto.ProcessingCoverPath
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
            .ThenInclude(m => m.CoverVotes)
            .FirstOrDefaultAsync(j => j.Id == id);
        if (job is null) return false;

        if (job.CoverMeta is not null)
        {
            context.CoverVoteMetas.RemoveRange(job.CoverMeta.CoverVotes);
            context.CoverMetas.Remove(job.CoverMeta);
        }

        job.CoverMetaId = null;

        await context.SaveChangesAsync();
        return true;
    }
}