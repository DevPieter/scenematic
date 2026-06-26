namespace Scenematic.Data.Models.DTOs.Cover;

public record RegisterCoverJobDto(
    string MetaHash,
    string AlbumTitle,
    string AlbumArtist,
    string AlbumCoverImageUrl,
    List<CoverVoteDto> CoverVotes
);