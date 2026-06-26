using System.ComponentModel.DataAnnotations;
using Scenematic.Common.Enums;

namespace Scenematic.Data.Models.Entities.Cover;

public class CoverJobFailure
{
    public Guid JobId { get; init; }

    public GenerateCoverJobFailureReason FailureReason { get; init; }

    [MaxLength(4000)]
    public string? ErrorMessage { get; init; }
}