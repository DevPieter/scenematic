using Scenematic.Worker.CoverGenerator.Models;
using SkiaSharp;

namespace Scenematic.Worker.CoverGenerator.Rendering.Overlays;

public sealed class SimpleTextOverlay(string text) : IOverlayRenderer
{
    private static readonly SKColor AccentColor = new(255, 184, 77);

    private SKSurface? _surface;
    private SKTypeface? _typeface;
    private SKFont? _font;
    private SKPaint? _surfacePaint;
    private SKPaint? _borderPaint;
    private SKPaint? _textPaint;
    private SKPaint? _accentPaint;

    public void Initialize(InitContext context)
    {
        _surface = SKSurface.Create(new SKImageInfo(context.Width, context.Height));
        _typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold);
        _font = new SKFont(_typeface, 13f);
        _surfacePaint = new SKPaint { IsAntialias = true };
        _borderPaint = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 1f };
        _textPaint = new SKPaint { IsAntialias = true };
        _accentPaint = new SKPaint { IsAntialias = true };
    }

    public bool ShouldRender(RenderContext context) => true;

    public byte[] Render(RenderContext context)
    {
        var canvas = _surface!.Canvas;
        canvas.Clear(SKColors.Transparent);

        var progress = EaseOutCubic(Math.Clamp(context.SceneProgress / 0.18f, 0f, 1f));
        var alpha = (byte)(255f * progress);
        var label = text.Trim().TrimEnd('.').ToUpperInvariant();
        var textWidth = _font!.MeasureText(label);
        var width = textWidth + 54f;
        var top = 32f + (1f - progress) * -8f;
        var badgeRect = new SKRect(context.Width - 34f - width, top, context.Width - 34f, top + 42f);

        _surfacePaint!.Color = new SKColor(15, 18, 25, (byte)(215 * progress));
        _borderPaint!.Color = new SKColor(255, 255, 255, (byte)(30 * progress));
        _textPaint!.Color = new SKColor(242, 244, 248, alpha);
        _accentPaint!.Color = AccentColor.WithAlpha(alpha);

        canvas.DrawRoundRect(badgeRect, 13f, 13f, _surfacePaint);
        canvas.DrawRoundRect(badgeRect, 13f, 13f, _borderPaint);
        canvas.DrawCircle(badgeRect.Left + 18f, badgeRect.MidY, 4f, _accentPaint);

        var baseline = badgeRect.MidY - (_font.Metrics.Ascent + _font.Metrics.Descent) / 2f;
        canvas.DrawText(label, badgeRect.Left + 31f, baseline, SKTextAlign.Left, _font, _textPaint);

        var underlineProgress = EaseInOutCubic(context.SceneProgress);
        var underlineRect = new SKRect(
            badgeRect.Left + 13f,
            badgeRect.Bottom - 4f,
            badgeRect.Left + 13f + (badgeRect.Width - 26f) * underlineProgress,
            badgeRect.Bottom - 2f
        );
        canvas.DrawRoundRect(underlineRect, 1f, 1f, _accentPaint);

        using var image = _surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Webp, Renderer.Quality);
        return data.ToArray();
    }

    public void Dispose()
    {
        _surface?.Dispose();
        _typeface?.Dispose();
        _font?.Dispose();
        _surfacePaint?.Dispose();
        _borderPaint?.Dispose();
        _textPaint?.Dispose();
        _accentPaint?.Dispose();
    }

    private static float EaseOutCubic(float value)
        => 1f - MathF.Pow(1f - value, 3f);

    private static float EaseInOutCubic(float value) => value < 0.5f
        ? 4f * value * value * value
        : 1f - MathF.Pow(-2f * value + 2f, 3f) / 2f;
}
