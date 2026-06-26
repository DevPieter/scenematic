using System.ComponentModel.DataAnnotations;

namespace Scenematic.Data.Models.Entities.Cover;

public class GenerateCoverJobSuccessResult
{
    public Guid JobId { get; init; }

    [MaxLength(256)]
    public string MainCoverPath { get; init; } = null!;

    [MaxLength(256)]
    public string DebounceCoverPath { get; init; } = null!;

    [MaxLength(256)]
    public string ProcessingCoverPath { get; init; } = null!;
}