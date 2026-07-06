using Scenematic.Data.Models.Entities.Cover;

namespace Scenematic.Data.Models.Entities.Webhooks;

public class CoverJobWebhook
{
    public Guid JobId { get; init; }
    public CoverJob Job { get; init; } = null!;

    public Guid WebhookId { get; init; }
    public Webhook Webhook { get; init; } = null!;
}