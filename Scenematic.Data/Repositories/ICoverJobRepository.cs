using Scenematic.Data.Models.DTOs.Cover;
using Scenematic.Data.Models.Entities.Cover;

namespace Scenematic.Data.Repositories;

public interface ICoverJobRepository
{
    Task<GenerateCoverJob> RegisterAsync(
        RegisterGenerateCoverJobDto jobDto
    );

    Task<GenerateCoverJob?> GetByIdAsync(
        Guid id,
        bool includeMetadata = false,
        bool includeResults = false
    );

    Task<GenerateCoverJob?> GetByMetaHashAsync(
        string metaHash,
        bool includeMetadata = false,
        bool includeResults = false
    );

    Task<bool> UpdateAsync(
        Guid id,
        UpdateGenerateCoverJobDto jobDto
    );

    Task<bool> HasSuccessAsync(
        Guid id
    );

    Task<GenerateCoverJobSuccessResult?> SetSuccessAsync(
        Guid id,
        SetGenerateCoverJobSuccessDto dto
    );

    Task<bool> HasFailureAsync(
        Guid id
    );

    Task<GenerateCoverJobFailureResult?> SetFailureAsync(
        Guid id,
        SetGenerateCoverJobFailureDto dto
    );

    Task<bool> DeleteMetaAsync(
        Guid id
    );
}