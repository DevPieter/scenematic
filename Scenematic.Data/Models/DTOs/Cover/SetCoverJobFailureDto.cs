using Scenematic.Common.Enums;

namespace Scenematic.Data.Models.DTOs.Cover;

public record SetCoverJobFailureDto(
    GenerateCoverJobFailureReason FailureReason,
    string? ErrorMessage
);