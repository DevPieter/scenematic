using Scenematic.Common.Enums;
using Scenematic.Common.Enums.Common;
using Scenematic.Common.Models.Common;

namespace Scenematic.Worker.CoverGenerator.Models.Covers;

public record ProcessCoverResult(
    ResultType Type,
    GenerateCoverJobFailureReason FailureReason,
    string? Message,
    string? MainCoverPath
    // string? DebounceCoverPath,
    // string? ProcessingCoverPath
) : Result(Type, Message)
{
    public static ProcessCoverResult Success(
        string mainCoverPath,
        string debounceCoverPath,
        string processingCoverPath
    ) => new(
        ResultType.Success,
        GenerateCoverJobFailureReason.None,
        "Successfully processed cover",
        mainCoverPath
        // debounceCoverPath,
        // processingCoverPath
    );

    public static ProcessCoverResult Failure(
        GenerateCoverJobFailureReason failureReason,
        string message
    ) => new(
        ResultType.InternalError,
        failureReason,
        message,
        null
    );
}