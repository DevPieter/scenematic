using Scenematic.Common.Enums.Songs;

namespace Scenematic.Data.Models.DTOs.Covers;

public record GenerateCoverVoteDto(
    SongVoteLevel VoteLevel,
    string AvatarUrl
);