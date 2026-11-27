using Scenematic.Data.Models.DTOs.Cover;

namespace Scenematic.Worker.CoverGenerator.Models.Covers;

public record ProcessCoverArgs(
    Guid JobId,
    string MetaHash,
    string AlbumTitle,
    string AlbumArtist,
    string AlbumCoverImageUrl
    // List<CoverVoteDto> CoverVotes
);