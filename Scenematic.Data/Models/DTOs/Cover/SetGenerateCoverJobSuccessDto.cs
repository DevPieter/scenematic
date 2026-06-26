namespace Scenematic.Data.Models.DTOs.Cover;

public record SetGenerateCoverJobSuccessDto(
    string MainCoverPath,
    string DebounceCoverPath,
    string ProcessingCoverPath
);