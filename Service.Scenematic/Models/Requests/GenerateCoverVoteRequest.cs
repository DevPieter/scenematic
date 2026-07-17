using Scenematic.Common.Enums.Songs;

namespace Service.Scenematic.Models.Requests;

public record GenerateCoverVoteRequest(
    SongVoteLevel VoteLevel,
    string AvatarUrl
);