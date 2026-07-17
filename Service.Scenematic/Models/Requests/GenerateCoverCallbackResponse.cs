namespace Service.Scenematic.Models.Requests;

public record GenerateCoverCallbackResponse(
    string Url,
    string Secret
);