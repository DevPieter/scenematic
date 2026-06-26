namespace Scenematic.Data.Models.DTOs.Cover;

public record RegisterGenerateCoverJobDto(
    string MetaHash,
    string AlbumTitle,
    string AlbumArtist,
    string AlbumCoverImageUrl,
    List<CoverVoteDto> CoverVotes
);