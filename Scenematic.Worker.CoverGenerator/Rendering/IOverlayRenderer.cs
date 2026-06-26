using Scenematic.Worker.CoverGenerator.Models;

namespace Scenematic.Worker.CoverGenerator.Rendering;

public interface IOverlayRenderer : IRenderer<InitContext, RenderContext>
{
    bool ShouldRender(RenderContext context);
}