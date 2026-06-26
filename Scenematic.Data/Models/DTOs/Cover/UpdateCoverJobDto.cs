using Scenematic.Common.Enums;

namespace Scenematic.Data.Models.DTOs.Cover;

public class UpdateCoverJobDto(
    GenerateCoverJobState state,
    DateTime? queuedAt,
    DateTime? startedAt,
    DateTime? completedAt,
    DateTime? failedAt
)
{
    public GenerateCoverJobState State { get; set; } = state;
    public DateTime? QueuedAt { get; set; } = queuedAt;
    public DateTime? StartedAt { get; set; } = startedAt;
    public DateTime? CompletedAt { get; set; } = completedAt;
    public DateTime? FailedAt { get; set; } = failedAt;
}