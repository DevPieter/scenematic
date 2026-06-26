using System.ComponentModel.DataAnnotations;
using Scenematic.Common.Enums;

namespace Scenematic.Data.Models.Entities.Cover;

public class GenerateCoverJob
{
    public Guid Id { get; init; }

    public Guid? CoverMetaId { get; set; }
    public CoverMeta? CoverMeta { get; set; }

    [MaxLength(128)]
    public string CoverMetaHash { get; init; } = null!;

    public GenerateCoverJobState State { get; set; }

    public GenerateCoverJobSuccessResult? SuccessResult { get; set; }
    public GenerateCoverJobFailureResult? FailureResult { get; set; }

    [Required]
    public DateTime CreatedAt { get; init; }

    public DateTime? QueuedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? FailedAt { get; set; }
}