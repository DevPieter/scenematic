using Scenematic.Common.Enums;

namespace Scenematic.Data.Models.DTOs.Covers;

public record SetGenerateCoverJobFailureDto(
    GenerateCoverJobFailureReason FailureReason,
    string? ErrorMessage
);