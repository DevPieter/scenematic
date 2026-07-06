using Scenematic.Common.Annotations;
using Scenematic.Data.Models.DTOs.Cover;

namespace Service.Scenematic;

public record RegisterCoverRequest(
    string Title,
    string Artist,
    [ValidWebUrl]
    string CoverImageUrl,
    [ValidWebUrl]
    string CallbackUrl,
    List<CoverVoteDto> CoverVotes
);