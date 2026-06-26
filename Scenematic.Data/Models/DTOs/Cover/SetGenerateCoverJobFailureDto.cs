using Scenematic.Common.Enums;

namespace Scenematic.Data.Models.DTOs.Cover;

public record SetGenerateCoverJobFailureDto(
    GenerateCoverJobFailureReason FailureReason,
    string? ErrorMessage
);