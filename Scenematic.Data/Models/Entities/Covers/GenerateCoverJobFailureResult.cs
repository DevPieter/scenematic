using System.ComponentModel.DataAnnotations;
using Scenematic.Common.Enums;

namespace Scenematic.Data.Models.Entities.Covers;

public class GenerateCoverJobFailureResult
{
    [Required]
    public Guid JobId { get; init; }

    [Required]
    public GenerateCoverJobFailureReason FailureReason { get; init; }

    [MaxLength(4000)]
    public string? ErrorMessage { get; init; }
}