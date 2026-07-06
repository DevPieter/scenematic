namespace Scenematic.Data.Models.Entities.Jobs;

public abstract class Job
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    public DateTime? QueuedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? FailedAt { get; set; }
}