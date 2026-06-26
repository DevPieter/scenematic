namespace Scenematic.Worker.CoverGenerator.Rendering;

public interface IRenderer<TI, TC> : IDisposable
{
    void Initialize(TI context);

    byte[] Render(TC context);
}