using System.ComponentModel.DataAnnotations;
using Scenematic.Common.Enums;

namespace Scenematic.Data.Models.Entities.Cover;

public class CoverJobFailure
{
    public Guid JobId { get; init; }

    public GenerateCoverJobFailureReason FailureReason { get; init; }

    [MaxLength(4000)]
    public string? ErrorMessage { get; init; }

    private CoverJobFailure()
    {
    }

    public CoverJobFailure(Guid jobId, GenerateCoverJobFailureReason failureReason, string? errorMessage)
    {
        JobId = jobId;
        FailureReason = failureReason;
        ErrorMessage = errorMessage;
    }
}