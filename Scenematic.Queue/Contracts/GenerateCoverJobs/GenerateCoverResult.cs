namespace Scenematic.Queue.Contracts.GenerateCoverJobs;

public record GenerateCoverResult(
    Guid JobId,
    bool Success
);