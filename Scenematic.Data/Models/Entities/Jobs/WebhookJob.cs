namespace Scenematic.Data.Models.Entities.Jobs;

public abstract class WebhookJob<T> : Job
{
    public ICollection<T> Webhooks { get; set; } = new List<T>();
}