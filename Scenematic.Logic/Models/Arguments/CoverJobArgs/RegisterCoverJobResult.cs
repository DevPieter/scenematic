using Scenematic.Data.Models.Entities.Cover;
using Scenematic.Data.Models.Entities.Webhooks;

namespace Scenematic.Logic.Models.Arguments.CoverJobArgs;

public record RegisterCoverJobResult(
    CoverJob Job,
    Webhook? Webhook
);