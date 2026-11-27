using Scenematic.Worker.CoverGenerator.Rendering.Utils;

namespace Scenematic.Worker.CoverGenerator.Rendering.Scenes;

using CoverGenerator;
using Models;
using Rendering;
using SkiaSharp;

public class CoverRenderer : ISceneRenderer
{
    private const float Padding = 34f;
    private const float ArtworkSize = 332f;
    private const float CornerRadius = 26f;

    private readonly string _title;
    private readonly string _author;
    private readonly SKImage _coverImage;
    private readonly SKSamplingOptions _sampling = new(SKCubicResampler.Mitchell);
    private readonly SKTypeface _regularTypeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Normal);
    private readonly SKTypeface _boldTypeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold);
    private readonly SKFont _titleFont;
    private readonly SKFont _authorFont;
    private readonly SKPaint _backgroundPaint = new() { IsAntialias = true };
    private readonly SKPaint _imagePaint = new() { IsAntialias = true };
    private readonly SKPaint _titlePaint = new() { IsAntialias = true };
    private readonly SKPaint _authorPaint = new() { IsAntialias = true };

    private readonly SKPaint _borderPaint = new()
    {
        IsAntialias = true,
        Style = SKPaintStyle.Stroke,
        StrokeWidth = 1.25f
    };

    private readonly SKPaint _shadowPaint = new() { IsAntialias = true };

    private SKSurface? _surface;
    private SKRect _backgroundRect;
    private SKRect _artworkRect;
    private float _textLeft;
    private List<string> _titleLines = [];

    public CoverRenderer(string title, string author, SKImage coverImage)
    {
        _title = title;
        _author = author;
        _coverImage = coverImage;
        _titleFont = new SKFont(_boldTypeface, 52f);
        _authorFont = new SKFont(_regularTypeface, 29f);
        _backgroundPaint.ImageFilter = SKImageFilter.CreateBlur(24f, 24f);
        _backgroundPaint.Color = new SKColor(255, 255, 255, 205);
        _shadowPaint.Color = new SKColor(0, 0, 0, 125);
        _shadowPaint.MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, 14f);
    }

    public void Initialize(InitContext context)
    {
        _surface = SKSurface.Create(new SKImageInfo(context.Width, context.Height));
        _backgroundRect = DrawingUtils.CoverRect(
            _coverImage.Width,
            _coverImage.Height,
            context.Width,
            context.Height
        );
        _artworkRect = new SKRect(Padding, Padding, Padding + ArtworkSize, Padding + ArtworkSize);
        _textLeft = _artworkRect.Right + 48f;
        _titleLines = DrawingUtils.WrapText(
            _title,
            _titleFont,
            context.Width - Padding - _textLeft,
            2
        );
    }

    public byte[] Render(RenderContext context)
    {
        var canvas = _surface!.Canvas;
        canvas.Clear(new SKColor(8, 10, 15));
        DrawBackground(canvas, context);

        var artworkProgress = 0.45f + 0.55f * DrawingUtils.EaseOutCubic(
            DrawingUtils.Normalize(context.SceneProgress, 0f, 0.22f)
        );
        var textProgress = DrawingUtils.EaseOutCubic(
            DrawingUtils.Normalize(context.SceneProgress, 0.05f, 0.28f)
        );

        DrawArtwork(canvas, artworkProgress);
        DrawText(canvas, textProgress, context.Height);

        using var image = _surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Webp, Renderer.Quality);
        return data.ToArray();
    }

    public void Dispose()
    {
        _surface?.Dispose();
        _regularTypeface.Dispose();
        _boldTypeface.Dispose();
        _titleFont.Dispose();
        _authorFont.Dispose();
        _backgroundPaint.Dispose();
        _imagePaint.Dispose();
        _titlePaint.Dispose();
        _authorPaint.Dispose();
        _borderPaint.Dispose();
        _shadowPaint.Dispose();
    }

    private void DrawBackground(SKCanvas canvas, RenderContext context)
    {
        var scale = 1.035f + DrawingUtils.EaseInOutCubic(context.SceneProgress) * 0.012f;
        canvas.Save();
        canvas.Scale(scale, scale, context.Width / 2f, context.Height / 2f);
        canvas.DrawImage(_coverImage, _backgroundRect, _sampling, _backgroundPaint);
        canvas.Restore();

        using var shade = new SKPaint
        {
            Shader = SKShader.CreateLinearGradient(
                new SKPoint(0f, 0f),
                new SKPoint(context.Width, 0f),
                [new SKColor(6, 8, 13, 170), new SKColor(8, 10, 16, 222)],
                SKShaderTileMode.Clamp
            )
        };
        canvas.DrawRect(0f, 0f, context.Width, context.Height, shade);
    }

    private void DrawArtwork(SKCanvas canvas, float progress)
    {
        var alpha = DrawingUtils.Alpha(progress);
        var scale = 0.975f + progress * 0.025f;

        canvas.Save();
        canvas.Translate((1f - progress) * -18f, (1f - progress) * 8f);
        canvas.Scale(scale, scale, _artworkRect.MidX, _artworkRect.MidY);
        canvas.DrawRoundRect(
            DrawingUtils.Offset(_artworkRect, 0f, 8f),
            CornerRadius,
            CornerRadius,
            _shadowPaint
        );

        using var path = new SKPath();
        path.AddRoundRect(_artworkRect, CornerRadius, CornerRadius);
        canvas.Save();
        canvas.ClipPath(path, antialias: true);
        _imagePaint.Color = SKColors.White.WithAlpha(alpha);
        canvas.DrawImage(
            _coverImage,
            DrawingUtils.CenterCropRect(_coverImage.Width, _coverImage.Height),
            _artworkRect,
            _sampling,
            _imagePaint
        );
        canvas.Restore();

        _borderPaint.Color = new SKColor(255, 255, 255, (byte)(42f * progress));
        canvas.DrawRoundRect(_artworkRect, CornerRadius, CornerRadius, _borderPaint);
        canvas.Restore();
    }

    private void DrawText(SKCanvas canvas, float progress, int height)
    {
        var alpha = DrawingUtils.Alpha(progress);
        var x = _textLeft + (1f - progress) * 18f;
        _titlePaint.Color = new SKColor(248, 249, 252, alpha);
        _authorPaint.Color = new SKColor(190, 196, 208, alpha);

        const float titleLineHeight = 59f;
        var totalHeight = _titleLines.Count * titleLineHeight + 18f + 36f;
        var y = (height - totalHeight) / 2f - _titleFont.Metrics.Ascent;

        foreach (var line in _titleLines)
        {
            canvas.DrawText(line, x, y, SKTextAlign.Left, _titleFont, _titlePaint);
            y += titleLineHeight;
        }

        y = y - titleLineHeight + _titleFont.Metrics.Descent + 18f - _authorFont.Metrics.Ascent;
        canvas.DrawText(_author, x, y, SKTextAlign.Left, _authorFont, _authorPaint);
    }
}