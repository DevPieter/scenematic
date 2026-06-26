using Scenematic.Common.Enums.Songs;

namespace Scenematic.Data.Models.DTOs.Cover;

public record CoverVoteDto(
    SongVoteLevel VoteLevel,
    string AvatarUrl
);