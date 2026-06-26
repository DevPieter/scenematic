using System.ComponentModel.DataAnnotations;

namespace Scenematic.Data.Models.Entities.Cover;

public class CoverMeta
{
    public Guid Id { get; init; }

    [MaxLength(128)]
    public string AlbumTitle { get; init; } = null!;

    [MaxLength(128)]
    public string AlbumArtist { get; init; } = null!;

    [MaxLength(256)]
    public string AlbumCoverImageUrl { get; init; } = null!;

    public ICollection<CoverVoteMeta> CoverVotes { get; set; } = new List<CoverVoteMeta>();
}