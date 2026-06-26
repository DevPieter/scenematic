using Scenematic.Common.Enums.Songs;

namespace Scenematic.Worker.CoverGenerator.Models;

public record SongVoteHolder(
    SongVoteLevel Level,
    string AvatarUrl
);