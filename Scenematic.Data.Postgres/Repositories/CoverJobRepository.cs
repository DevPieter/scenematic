using Microsoft.EntityFrameworkCore;
using Scenematic.Common.Enums;
using Scenematic.Data.Models.DTOs.Cover;
using Scenematic.Data.Models.Entities.Cover;
using Scenematic.Data.Postgres.Data;
using Scenematic.Data.Repositories;

namespace Scenematic.Data.Postgres.Repositories;

public class CoverJobRepository(ScenematicNpgsqlDataContext context) : ICoverJobRepository
{
    public async Task<CoverJob> RegisterAsync(RegisterCoverJobDto jobDto)
    {
        var job = new CoverJob()
        {
            CoverMetaHash = jobDto.MetaHash,
            State = GenerateCoverJobState.Pending,
            CreatedAt = DateTime.UtcNow,
        };

        await context.GenerateCoverJobs.AddAsync(job);
        await context.SaveChangesAsync();

        var meta = new CoverMeta()
        {
            AlbumTitle = jobDto.AlbumTitle,
            AlbumArtist = jobDto.AlbumArtist,
            AlbumCoverImageUrl = jobDto.AlbumCoverImageUrl,
            CoverVotes = jobDto.CoverVotes.ConvertAll(cv => new CoverVoteMeta()
            {
                VoteLevel = cv.VoteLevel,
                AvatarUrl = cv.AvatarUrl
            })
        };

        job.CoverMeta = meta;
        await context.SaveChangesAsync();

        return job;
    }

    public async Task<CoverJob?> GetByIdAsync(
        Guid id,
        bool includeMetadata = false,
        bool includeResults = false
    )
    {
        var query = context.GenerateCoverJobs.AsNoTracking();

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

        return await query.FirstOrDefaultAsync(j => j.Id == id);
    }

    public async Task<CoverJob?> GetByMetaHashAsync(
        string metaHash,
        bool includeMetadata = false,
        bool includeResults = false
    )
    {
        var query = context.GenerateCoverJobs.AsNoTracking();

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

        return await query.FirstOrDefaultAsync(j => j.CoverMetaHash == metaHash);
    }

    public async Task<bool> UpdateAsync(Guid id, UpdateCoverJobDto jobDto)
    {
        var job = await context.GenerateCoverJobs.FirstOrDefaultAsync(j => j.Id == id);
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
        var result = await context.GenerateCoverJobSuccessResults.AsNoTracking()
            .FirstOrDefaultAsync(r => r.JobId == id);

        return result is not null;
    }

    public async Task<CoverJobSuccess?> SetSuccessAsync(Guid id, SetCoverJobSuccessDto dto)
    {
        var job = await context.GenerateCoverJobs.FirstOrDefaultAsync(j => j.Id == id);
        if (job is null) return null;

        var result = new CoverJobSuccess()
        {
            JobId = id,
            MainCoverPath = dto.MainCoverPath,
            DebounceCoverPath = dto.DebounceCoverPath,
            ProcessingCoverPath = dto.ProcessingCoverPath
        };

        job.SuccessResult = result;

        await context.SaveChangesAsync();
        return result;
    }

    public async Task<bool> HasFailureAsync(Guid id)
    {
        var result = await context.GenerateCoverJobFailureResults.AsNoTracking()
            .FirstOrDefaultAsync(r => r.JobId == id);

        return result is not null;
    }

    public async Task<CoverJobFailure?> SetFailureAsync(Guid id, SetCoverJobFailureDto dto)
    {
        var job = await context.GenerateCoverJobs.FirstOrDefaultAsync(j => j.Id == id);
        if (job is null) return null;

        var result = new CoverJobFailure()
        {
            JobId = id,
            FailureReason = dto.FailureReason,
            ErrorMessage = dto.ErrorMessage
        };

        job.FailureResult = result;

        await context.SaveChangesAsync();
        return result;
    }

    public async Task<bool> DeleteMetaAsync(Guid id)
    {
        var job = await context.GenerateCoverJobs
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