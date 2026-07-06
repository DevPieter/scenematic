using System.ComponentModel.DataAnnotations;
using Scenematic.Common.Enums;
using Scenematic.Data.Models.Entities.Jobs;
using Scenematic.Data.Models.Entities.Webhooks;

namespace Scenematic.Data.Models.Entities.Cover;

public class CoverJob : WebhookJob<CoverJobWebhook>
{
    public Guid? CoverMetaId { get; set; }
    public CoverMeta? CoverMeta { get; set; }

    [MaxLength(128)]
    public string CoverMetaHash { get; init; } = null!;

    public GenerateCoverJobState State { get; set; } = GenerateCoverJobState.Pending;

    public CoverJobSuccess? SuccessResult { get; set; }
    public CoverJobFailure? FailureResult { get; set; }

    private CoverJob()
    {
    }

    public CoverJob(string coverMetaHash)
    {
        CoverMetaHash = coverMetaHash;
    }
}