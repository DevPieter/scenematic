using Scenematic.Common.Enums;

namespace Service.Scenematic.Models.Requests;

public record GenerateCoverResponse(
    Guid JobId,
    GenerateCoverJobState State,
    GenerateCoverSuccessResultResponse? SuccessResult,
    GenerateCoverFailureResultResponse? FailureResult,
    GenerateCoverCallbackResponse? Callback
);