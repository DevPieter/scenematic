using System.ComponentModel.DataAnnotations;
using Scenematic.Common.Enums.Songs;

namespace Scenematic.Data.Models.Entities.Cover;

public class CoverVoteMeta
{
    public Guid Id { get; init; }

    public Guid CoverMetaId { get; init; }
    public CoverMeta CoverMeta { get; init; } = null!;

    public SongVoteLevel VoteLevel { get; init; }

    [MaxLength(256)]
    public string AvatarUrl { get; init; } = null!;
}