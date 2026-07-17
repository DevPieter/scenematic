namespace Service.Scenematic.Models.Requests;

public record GenerateCoverFailureResultResponse(
    string? ErrorMessage,
    DateTime FailedAt
);