namespace Service.Scenematic.Models.Requests;

public record GenerateCoverSuccessResultResponse(
    string MainCoverPath,
    // string DebounceCoverPath,
    // string ProcessingCoverPath,
    DateTime CompletedAt
);