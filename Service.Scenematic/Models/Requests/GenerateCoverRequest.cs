using Scenematic.Common.Annotations;

namespace Service.Scenematic.Models.Requests;

public record GenerateCoverRequest(
    string Title,
    string Artist,
    [ValidWebUrl]
    string CoverImageUrl,
    [ValidWebUrl]
    string? CallbackUrl
    // List<GenerateCoverVoteRequest> CoverVotes
);