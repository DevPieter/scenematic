using Scenematic.Data.Models.DTOs.Cover;

namespace Scenematic.Logic.Models.Arguments.CoverJobArgs;

public record RegisterCoverJobArgs(
    string Title,
    string Artist,
    string CoverImageUrl,
    // List<CoverVoteDto> CoverVotes,
    string? CallbackUrl
);