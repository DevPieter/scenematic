using Scenematic.Worker.CoverGenerator.Models;
using SkiaSharp;

namespace Scenematic.Worker.CoverGenerator.Rendering.Overlays;

public sealed class BrandingOverlay(
    string brandName = "Tijdperk",
    string prefix = "Powered by"
) : IOverlayRenderer
{
    private static readonly SKColor AccentColor = new(255, 184, 77);

    private SKSurface? _surface;
    private SKTypeface? _regularTypeface;
    private SKTypeface? _boldTypeface;
    private SKFont? _prefixFont;
    private SKFont? _brandFont;
    private SKPaint? _surfacePaint;
    private SKPaint? _borderPaint;
    private SKPaint? _prefixPaint;
    private SKPaint? _brandPaint;
    private SKPaint? _accentPaint;

    public void Initialize(InitContext context)
    {
        _surface = SKSurface.Create(new SKImageInfo(context.Width, context.Height));
        _regularTypeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Normal);
        _boldTypeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold);
        _prefixFont = new SKFont(_regularTypeface, 12f);
        _brandFont = new SKFont(_boldTypeface, 14f);

        _surfacePaint = new SKPaint { IsAntialias = true };
        _borderPaint = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 1f };
        _prefixPaint = new SKPaint { IsAntialias = true };
        _brandPaint = new SKPaint { IsAntialias = true };
        _accentPaint = new SKPaint { IsAntialias = true };
    }

    public bool ShouldRender(RenderContext context) => true;

    public byte[] Render(RenderContext context)
    {
        var canvas = _surface!.Canvas;
        canvas.Clear(SKColors.Transparent);

        var enter = EaseOutCubic(Normalize(context.SceneProgress, 0f, 0.20f));
        var exit = EaseInOutCubic(Normalize(context.SceneProgress, 0.72f, 1f));
        var visibility = enter * (1f - exit);
        if (visibility > 0f) DrawBadge(canvas, context, visibility);

        using var image = _surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Webp, Renderer.Quality);
        return data.ToArray();
    }

    public void Dispose()
    {
        _surface?.Dispose();
        _regularTypeface?.Dispose();
        _boldTypeface?.Dispose();
        _prefixFont?.Dispose();
        _brandFont?.Dispose();
        _surfacePaint?.Dispose();
        _borderPaint?.Dispose();
        _prefixPaint?.Dispose();
        _brandPaint?.Dispose();
        _accentPaint?.Dispose();
    }

    private void DrawBadge(SKCanvas canvas, RenderContext context, float visibility)
    {
        var alpha = (byte)(255f * visibility);
        var prefixWidth = _prefixFont!.MeasureText(prefix);
        var brandWidth = _brandFont!.MeasureText(brandName);
        var width = prefixWidth + brandWidth + 47f;
        var top = 32f + (1f - visibility) * -8f;
        var badgeRect = new SKRect(context.Width - 34f - width, top, context.Width - 34f, top + 42f);

        _surfacePaint!.Color = new SKColor(15, 18, 25, (byte)(215 * visibility));
        _borderPaint!.Color = new SKColor(255, 255, 255, (byte)(30 * visibility));
        _prefixPaint!.Color = new SKColor(174, 181, 195, alpha);
        _brandPaint!.Color = SKColors.White.WithAlpha(alpha);
        _accentPaint!.Color = AccentColor.WithAlpha(alpha);

        canvas.DrawRoundRect(badgeRect, 13f, 13f, _surfacePaint);
        canvas.DrawRoundRect(badgeRect, 13f, 13f, _borderPaint);
        canvas.DrawRoundRect(
            new SKRect(badgeRect.Left + 12f, badgeRect.Top + 13f, badgeRect.Left + 15f, badgeRect.Bottom - 13f),
            1.5f,
            1.5f,
            _accentPaint
        );

        var baseline = badgeRect.MidY - (_prefixFont.Metrics.Ascent + _prefixFont.Metrics.Descent) / 2f;
        var x = badgeRect.Left + 24f;
        canvas.DrawText(prefix, x, baseline, SKTextAlign.Left, _prefixFont, _prefixPaint);
        canvas.DrawText(brandName, x + prefixWidth + 6f, baseline, SKTextAlign.Left, _brandFont, _brandPaint);
    }

    private static float Normalize(float value, float start, float end)
        => Math.Clamp((value - start) / (end - start), 0f, 1f);

    private static float EaseOutCubic(float value)
        => 1f - MathF.Pow(1f - value, 3f);

    private static float EaseInOutCubic(float value) => value < 0.5f
        ? 4f * value * value * value
        : 1f - MathF.Pow(-2f * value + 2f, 3f) / 2f;
}
