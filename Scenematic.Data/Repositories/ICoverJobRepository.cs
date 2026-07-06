using Scenematic.Data.Models.DTOs.Cover;
using Scenematic.Data.Models.Entities.Cover;

namespace Scenematic.Data.Repositories;

public interface ICoverJobRepository
{
    Task<CoverJob> RegisterAsync(
        RegisterCoverJobDto dto
    );

    Task<CoverJob?> GetByIdAsync(
        Guid id,
        bool includeMetadata = false,
        bool includeResults = false,
        bool includeWebhooks = false
    );

    Task<CoverJob?> GetByMetaHashAsync(
        string metaHash,
        bool includeMetadata = false,
        bool includeResults = false,
        bool includeWebhooks = false
    );

    Task<bool> UpdateAsync(
        Guid id,
        UpdateCoverJobDto jobDto
    );

    Task<bool> HasSuccessAsync(
        Guid id
    );

    Task<CoverJobSuccess?> SetSuccessAsync(
        Guid id,
        SetCoverJobSuccessDto dto
    );

    Task<bool> HasFailureAsync(
        Guid id
    );

    Task<CoverJobFailure?> SetFailureAsync(
        Guid id,
        SetCoverJobFailureDto dto
    );

    Task<bool> DeleteMetaAsync(
        Guid id
    );
}