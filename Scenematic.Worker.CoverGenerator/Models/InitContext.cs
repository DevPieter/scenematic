namespace Scenematic.Worker.CoverGenerator.Models;

public record InitContext(
    int Width,
    int Height,
    Scene Scene
);