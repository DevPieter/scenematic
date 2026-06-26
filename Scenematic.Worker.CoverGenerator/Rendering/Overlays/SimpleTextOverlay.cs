using Scenematic.Worker.CoverGenerator.Models;
using SkiaSharp;

namespace Scenematic.Worker.CoverGenerator.Rendering.Overlays;

public class SimpleTextOverlay(
    string text
) : IOverlayRenderer
{
    private SKSurface? _surface;

    private SKFont? _font;
    private SKPaint? _textPaint;

    public void Dispose()
    {
    }

    public void Initialize(InitContext context)
    {
        _surface = SKSurface.Create(new SKImageInfo(context.Width, context.Height));

        _font = new SKFont(SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold), 48);

        _textPaint = new SKPaint();
        _textPaint.Color = SKColors.White;
        _textPaint.IsAntialias = true;
    }

    public byte[] Render(RenderContext context)
    {
        var canvas = _surface!.Canvas;
        canvas.Clear(SKColors.Transparent);

        _font!.MeasureText(text, out var textBounds, _textPaint!);
        var x = context.Width - textBounds.Width - 20;
        var y = textBounds.Height + 20;

        canvas.DrawText(text, x, y, _font!, _textPaint!);

        using var image = _surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Webp, Renderer.Quality);
        return data.ToArray();
    }

    public bool ShouldRender(RenderContext context) => true;
}