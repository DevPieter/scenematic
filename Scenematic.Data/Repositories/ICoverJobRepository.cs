using Scenematic.Data.Models.DTOs.Cover;
using Scenematic.Data.Models.DTOs.Webhooks;
using Scenematic.Data.Models.Entities.Cover;
using Scenematic.Data.Models.Entities.Webhooks;

namespace Scenematic.Data.Repositories;

public interface ICoverJobRepository
{
    Task<CoverJob> RegisterAsync(
        RegisterCoverJobDto dto
    );

    Task<CoverJobWebhook?> RegisterWebhookAsync(
        Guid jobId,
        RegisterWebhookDto dto
    );

    Task<CoverJob?> GetByIdAsync(
        Guid id,
        bool includeMetadata = false,
        bool includeResults = false,
        bool includeWebhooks = false
    );

    Task<bool> DoesMetaHashExistAsync(
        string metaHash
    );

    Task<CoverJob?> GetByMetaHashAsync(
        string metaHash,
        bool includeMetadata = false,
        bool includeResults = false,
        bool includeWebhooks = false
    );

    Task<bool> DoesWebhookExistAsync(
        Guid jobId,
        string url
    );

    Task<List<Webhook>> GetWebhooksAsync(
        Guid jobId
    );

    Task MarkQueuedIfPendingAsync(Guid id);

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