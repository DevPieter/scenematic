namespace Scenematic.Data.Models.DTOs.Cover;

public record SetCoverJobSuccessDto(
    string MainCoverPath,
    string DebounceCoverPath,
    string ProcessingCoverPath
);