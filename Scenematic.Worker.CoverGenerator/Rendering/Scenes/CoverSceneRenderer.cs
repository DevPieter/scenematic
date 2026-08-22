using Scenematic.Common.Enums.Songs;
using Scenematic.Common.Extensions;
using Scenematic.Data.Models.DTOs.Cover;
using Scenematic.Worker.CoverGenerator.Models;
using SkiaSharp;

namespace Scenematic.Worker.CoverGenerator.Rendering.Scenes;

public sealed class CoverSceneRenderer : ISceneRenderer
{
    private const float Padding = 34f;
    private const float CoverSize = 382f;
    private const float CoverCornerRadius = 26f;
    private const float ContentGap = 38f;
    private const float VoteAvatarSize = 52f;
    private const float VoteAvatarStep = 32f;
    private const float VoteRailHeight = 12f;

    private static readonly SKColor AccentColor = new(255, 184, 77);
    private static readonly SKColor SurfaceColor = new(16, 19, 27, 218);
    private static readonly SKColor PrimaryTextColor = new(248, 249, 252);
    private static readonly SKColor SecondaryTextColor = new(190, 196, 208);

    private static readonly SongVoteLevel[] VoteLevels =
    [
        SongVoteLevel.Love,
        SongVoteLevel.Like,
        SongVoteLevel.Neutral,
        SongVoteLevel.Dislike,
        SongVoteLevel.Trash
    ];

    private readonly string _title;
    private readonly string _author;
    private readonly List<CoverVoteDto> _votes;
    private readonly SKImage _coverImage;
    private readonly Dictionary<string, SKImage> _avatarImages;
    private readonly SKSamplingOptions _samplingOptions = new(SKCubicResampler.Mitchell);

    private readonly SKTypeface _regularTypeface;
    private readonly SKTypeface _boldTypeface;
    private readonly SKFont _titleFont;
    private readonly SKFont _authorFont;
    private readonly SKFont _metaFont;
    private readonly SKFont _voteLabelFont;
    private readonly SKFont _overflowFont;

    private readonly SKPaint _backgroundPaint = new() { IsAntialias = true };
    private readonly SKPaint _coverPaint = new() { IsAntialias = true };
    private readonly SKPaint _primaryTextPaint = new() { IsAntialias = true };
    private readonly SKPaint _secondaryTextPaint = new() { IsAntialias = true };
    private readonly SKPaint _mutedTextPaint = new() { IsAntialias = true };
    private readonly SKPaint _surfacePaint = new() { IsAntialias = true };
    private readonly SKPaint _borderPaint = new() { IsAntialias = true, Style = SKPaintStyle.Stroke };
    private readonly SKPaint _shadowPaint = new() { IsAntialias = true };

    private SKSurface? _surface;
    private SKRect _backgroundRect;
    private SKRect _coverRect;
    private SKRect _contentRect;
    private SKRect _voteCardRect;
    private List<string> _titleLines = [];
    private List<string> _authorLines = [];

    public CoverSceneRenderer(
        string title,
        string author,
        List<CoverVoteDto> votes,
        SKImage coverImage,
        Dictionary<string, SKImage> avatarImages
    )
    {
        _title = title;
        _author = author;
        _votes = votes;
        _coverImage = coverImage;
        _avatarImages = avatarImages;

        _regularTypeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Normal);
        _boldTypeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold);
        _titleFont = new SKFont(_boldTypeface, 43f);
        _authorFont = new SKFont(_regularTypeface, 25f);
        _metaFont = new SKFont(_boldTypeface, 14f);
        _voteLabelFont = new SKFont(_boldTypeface, 14f);
        _overflowFont = new SKFont(_boldTypeface, 14f);

        _backgroundPaint.ImageFilter = SKImageFilter.CreateBlur(24f, 24f);
        _backgroundPaint.Color = new SKColor(255, 255, 255, 205);
        _primaryTextPaint.Color = PrimaryTextColor;
        _secondaryTextPaint.Color = SecondaryTextColor;
        _mutedTextPaint.Color = new SKColor(151, 158, 173);
        _surfacePaint.Color = SurfaceColor;
        _borderPaint.Color = new SKColor(255, 255, 255, 26);
        _borderPaint.StrokeWidth = 1f;
        _shadowPaint.Color = new SKColor(0, 0, 0, 125);
        _shadowPaint.MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, 14f);
    }

    public void Initialize(InitContext context)
    {
        _surface = SKSurface.Create(new SKImageInfo(context.Width, context.Height));
        _backgroundRect = CoverRect(_coverImage.Width, _coverImage.Height, context.Width, context.Height);
        _coverRect = new SKRect(Padding, Padding, Padding + CoverSize, Padding + CoverSize);
        _contentRect = new SKRect(
            _coverRect.Right + ContentGap,
            Padding,
            context.Width - Padding,
            context.Height - Padding
        );
        _voteCardRect = new SKRect(_contentRect.Left, 230f, _contentRect.Right, context.Height - Padding);

        _titleLines = WrapText(_title, _titleFont, _contentRect.Width, 2);
        _authorLines = WrapText(_author, _authorFont, _contentRect.Width, 1);
    }

    public byte[] Render(RenderContext context)
    {
        var canvas = _surface!.Canvas;
        canvas.Clear(new SKColor(8, 10, 15));

        DrawBackground(canvas, context);

        var coverProgress = 0.42f + 0.58f * EaseOutCubic(Normalize(context.SceneProgress, 0f, 0.22f));
        var textProgress = EaseOutCubic(Normalize(context.SceneProgress, 0.05f, 0.28f));
        var cardProgress = EaseOutCubic(Normalize(context.SceneProgress, 0.12f, 0.36f));

        DrawCover(canvas, coverProgress);
        DrawMetadata(canvas, textProgress);
        DrawVoteCard(canvas, context, cardProgress);

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
        _metaFont.Dispose();
        _voteLabelFont.Dispose();
        _overflowFont.Dispose();
        _backgroundPaint.Dispose();
        _coverPaint.Dispose();
        _primaryTextPaint.Dispose();
        _secondaryTextPaint.Dispose();
        _mutedTextPaint.Dispose();
        _surfacePaint.Dispose();
        _borderPaint.Dispose();
        _shadowPaint.Dispose();
    }

    private void DrawBackground(SKCanvas canvas, RenderContext context)
    {
        var drift = EaseInOutCubic(context.SceneProgress);
        var scale = 1.035f + drift * 0.012f;

        canvas.Save();
        canvas.Scale(scale, scale, context.Width / 2f, context.Height / 2f);
        canvas.DrawImage(_coverImage, _backgroundRect, _samplingOptions, _backgroundPaint);
        canvas.Restore();

        using var horizontalShade = new SKPaint
        {
            Shader = SKShader.CreateLinearGradient(
                new SKPoint(0f, 0f),
                new SKPoint(context.Width, 0f),
                [new SKColor(6, 8, 13, 170), new SKColor(8, 10, 16, 218)],
                SKShaderTileMode.Clamp
            )
        };
        canvas.DrawRect(0f, 0f, context.Width, context.Height, horizontalShade);

        using var verticalShade = new SKPaint
        {
            Shader = SKShader.CreateLinearGradient(
                new SKPoint(0f, 0f),
                new SKPoint(0f, context.Height),
                [new SKColor(6, 8, 13, 10), new SKColor(6, 8, 13, 115)],
                SKShaderTileMode.Clamp
            )
        };
        canvas.DrawRect(0f, 0f, context.Width, context.Height, verticalShade);

        using var glowPaint = new SKPaint
        {
            Shader = SKShader.CreateRadialGradient(
                new SKPoint(_coverRect.MidX, _coverRect.MidY),
                350f,
                [new SKColor(255, 184, 77, 25), SKColors.Transparent],
                SKShaderTileMode.Clamp
            )
        };
        canvas.DrawRect(0f, 0f, context.Width, context.Height, glowPaint);
    }

    private void DrawCover(SKCanvas canvas, float progress)
    {
        var offsetX = (1f - progress) * -18f;
        var offsetY = (1f - progress) * 8f;
        var scale = 0.975f + progress * 0.025f;
        var alpha = Alpha(progress);

        canvas.Save();
        canvas.Translate(offsetX, offsetY);
        canvas.Scale(scale, scale, _coverRect.MidX, _coverRect.MidY);

        _shadowPaint.Color = new SKColor(0, 0, 0, (byte)(125 * progress));
        canvas.DrawRoundRect(Offset(_coverRect, 0f, 8f), CoverCornerRadius, CoverCornerRadius, _shadowPaint);

        using var coverPath = new SKPath();
        coverPath.AddRoundRect(_coverRect, CoverCornerRadius, CoverCornerRadius);

        canvas.Save();
        canvas.ClipPath(coverPath, antialias: true);
        _coverPaint.Color = new SKColor(255, 255, 255, alpha);
        canvas.DrawImage(_coverImage, CenterCropRect(_coverImage.Width, _coverImage.Height), _coverRect, _samplingOptions,
            _coverPaint);
        canvas.Restore();

        _borderPaint.Color = new SKColor(255, 255, 255, (byte)(42 * progress));
        _borderPaint.StrokeWidth = 1.25f;
        canvas.DrawRoundRect(_coverRect, CoverCornerRadius, CoverCornerRadius, _borderPaint);
        canvas.Restore();
    }

    private void DrawMetadata(SKCanvas canvas, float progress)
    {
        var alpha = Alpha(progress);
        var x = _contentRect.Left + (1f - progress) * 18f;

        _primaryTextPaint.Color = PrimaryTextColor.WithAlpha(alpha);
        _secondaryTextPaint.Color = SecondaryTextColor.WithAlpha(alpha);

        const float titleLineHeight = 49f;
        var y = 62f - _titleFont.Metrics.Ascent;

        foreach (var line in _titleLines)
        {
            canvas.DrawText(line, x, y, SKTextAlign.Left, _titleFont, _primaryTextPaint);
            y += titleLineHeight;
        }

        y = y - titleLineHeight + _titleFont.Metrics.Descent + 14f - _authorFont.Metrics.Ascent;
        foreach (var line in _authorLines)
        {
            canvas.DrawText(line, x, y, SKTextAlign.Left, _authorFont, _secondaryTextPaint);
        }
    }

    private void DrawVoteCard(SKCanvas canvas, RenderContext context, float progress)
    {
        var alpha = Alpha(progress);
        var offsetY = (1f - progress) * 12f;

        canvas.Save();
        canvas.Translate(0f, offsetY);

        _surfacePaint.Color = SurfaceColor.WithAlpha((byte)(SurfaceColor.Alpha * progress));
        _borderPaint.Color = new SKColor(255, 255, 255, (byte)(28 * progress));
        _borderPaint.StrokeWidth = 1f;
        canvas.DrawRoundRect(_voteCardRect, 18f, 18f, _surfacePaint);
        canvas.DrawRoundRect(_voteCardRect, 18f, 18f, _borderPaint);

        _mutedTextPaint.Color = new SKColor(157, 164, 179, alpha);
        canvas.DrawText("COMMUNITY SCORE", _voteCardRect.Left + 22f, _voteCardRect.Top + 31f, SKTextAlign.Left,
            _metaFont, _mutedTextPaint);

        var winningLevel = _votes.Select(v => v.VoteLevel).ToList().CalculateLevel();
        var summary = _votes.Count == 0
            ? "NO VOTES YET"
            : $"{_votes.Count} {(_votes.Count == 1 ? "VOTE" : "VOTES")}  ·  {GetVoteLabel(winningLevel)}";

        _primaryTextPaint.Color = PrimaryTextColor.WithAlpha(alpha);
        canvas.DrawText(summary, _voteCardRect.Right - 22f, _voteCardRect.Top + 31f, SKTextAlign.Right, _metaFont,
            _primaryTextPaint);

        var railRect = new SKRect(
            _voteCardRect.Left + 22f,
            _voteCardRect.Top + 80f,
            _voteCardRect.Right - 22f,
            _voteCardRect.Top + 80f + VoteRailHeight
        );

        DrawVoteRail(canvas, railRect, winningLevel, context.SceneProgress, progress);
        DrawVoteAvatars(canvas, railRect, context.SceneProgress, progress);
        canvas.Restore();
    }

    private void DrawVoteRail(
        SKCanvas canvas,
        SKRect railRect,
        SongVoteLevel winningLevel,
        float sceneProgress,
        float cardProgress
    )
    {
        var segmentWidth = railRect.Width / VoteLevels.Length;
        var highlightProgress = EaseOutCubic(Normalize(sceneProgress, 0.24f, 0.55f));

        for (var i = 0; i < VoteLevels.Length; i++)
        {
            var level = VoteLevels[i];
            var isWinner = _votes.Count > 0 && level == winningLevel;
            var levelColor = GetVoteColor(level);
            var railAlpha = (isWinner ? 80f + 175f * highlightProgress : 44f) * cardProgress;

            using var segmentPaint = new SKPaint
            {
                Color = levelColor.WithAlpha((byte)Math.Clamp(railAlpha, 0f, 255f)),
                IsAntialias = true
            };

            var segmentRect = new SKRect(
                railRect.Left + i * segmentWidth + 2f,
                railRect.Top,
                railRect.Left + (i + 1) * segmentWidth - 2f,
                railRect.Bottom
            );
            canvas.DrawRoundRect(segmentRect, VoteRailHeight / 2f, VoteRailHeight / 2f, segmentPaint);

            var labelAlpha = (byte)((isWinner ? 230f : 118f) * cardProgress);
            _mutedTextPaint.Color = (isWinner ? PrimaryTextColor : SecondaryTextColor).WithAlpha(labelAlpha);
            canvas.DrawText(GetVoteLabel(level), segmentRect.MidX, railRect.Top - 14f, SKTextAlign.Center,
                _voteLabelFont, _mutedTextPaint);
        }
    }

    private void DrawVoteAvatars(SKCanvas canvas, SKRect railRect, float sceneProgress, float cardProgress)
    {
        var segmentWidth = railRect.Width / VoteLevels.Length;
        var maxVisible = Math.Max(1, (int)Math.Floor((segmentWidth - VoteAvatarSize) / VoteAvatarStep) + 1);
        var drawIndex = 0;

        for (var levelIndex = 0; levelIndex < VoteLevels.Length; levelIndex++)
        {
            var levelVotes = _votes.Where(v => v.VoteLevel == VoteLevels[levelIndex]).ToList();
            if (levelVotes.Count == 0) continue;

            var visibleVotes = levelVotes.Take(maxVisible).ToList();
            var hiddenCount = levelVotes.Count - visibleVotes.Count;
            if (hiddenCount > 0 && visibleVotes.Count > 1)
            {
                visibleVotes.RemoveAt(visibleVotes.Count - 1);
                hiddenCount++;
            }

            var chipCount = visibleVotes.Count + (hiddenCount > 0 ? 1 : 0);
            var totalWidth = VoteAvatarSize + (chipCount - 1) * VoteAvatarStep;
            var segmentCenterX = railRect.Left + levelIndex * segmentWidth + segmentWidth / 2f;
            var startX = segmentCenterX - totalWidth / 2f + VoteAvatarSize / 2f;

            for (var i = 0; i < visibleVotes.Count; i++)
            {
                var entryProgress = EaseOutCubic(Normalize(sceneProgress, 0.30f + drawIndex * 0.025f, 0.58f));
                var center = new SKPoint(
                    startX + i * VoteAvatarStep,
                    railRect.Bottom + 34f + (1f - entryProgress) * 8f
                );

                DrawAvatarChip(
                    canvas,
                    visibleVotes[i],
                    center,
                    GetVoteColor(VoteLevels[levelIndex]),
                    (byte)(255f * entryProgress * cardProgress)
                );
                drawIndex++;
            }

            if (hiddenCount <= 0) continue;

            var overflowProgress = EaseOutCubic(Normalize(sceneProgress, 0.30f + drawIndex * 0.025f, 0.58f));
            var overflowCenter = new SKPoint(
                startX + visibleVotes.Count * VoteAvatarStep,
                railRect.Bottom + 34f + (1f - overflowProgress) * 8f
            );
            DrawOverflowChip(
                canvas,
                hiddenCount,
                overflowCenter,
                GetVoteColor(VoteLevels[levelIndex]),
                (byte)(255f * overflowProgress * cardProgress)
            );
            drawIndex++;
        }
    }

    private void DrawAvatarChip(SKCanvas canvas, CoverVoteDto vote, SKPoint center, SKColor levelColor, byte alpha)
    {
        if (!_avatarImages.TryGetValue(vote.AvatarUrl, out var avatarImage) || alpha == 0) return;

        var radius = VoteAvatarSize / 2f;
        var avatarRect = new SKRect(center.X - radius, center.Y - radius, center.X + radius, center.Y + radius);

        using var shadowPaint = new SKPaint
        {
            Color = new SKColor(0, 0, 0, (byte)(85 * alpha / 255f)),
            IsAntialias = true,
            MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, 4f)
        };
        canvas.DrawCircle(center.X, center.Y + 2f, radius, shadowPaint);

        canvas.Save();
        using var avatarPath = new SKPath();
        avatarPath.AddCircle(center.X, center.Y, radius - 1f);
        canvas.ClipPath(avatarPath, antialias: true);
        using var imagePaint = new SKPaint { Color = SKColors.White.WithAlpha(alpha), IsAntialias = true };
        canvas.DrawImage(
            avatarImage,
            CenterCropRect(avatarImage.Width, avatarImage.Height),
            avatarRect,
            _samplingOptions,
            imagePaint
        );
        canvas.Restore();

        using var borderPaint = new SKPaint
        {
            Color = levelColor.WithAlpha(alpha),
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 2.5f
        };
        canvas.DrawCircle(center.X, center.Y, radius - 1.25f, borderPaint);
    }

    private void DrawOverflowChip(SKCanvas canvas, int hiddenCount, SKPoint center, SKColor levelColor, byte alpha)
    {
        if (alpha == 0) return;

        var radius = VoteAvatarSize / 2f;
        using var fillPaint = new SKPaint
        {
            Color = new SKColor(20, 23, 31, (byte)(235 * alpha / 255f)),
            IsAntialias = true
        };
        using var borderPaint = new SKPaint
        {
            Color = levelColor.WithAlpha(alpha),
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 2.5f
        };
        using var textPaint = new SKPaint { Color = PrimaryTextColor.WithAlpha(alpha), IsAntialias = true };

        canvas.DrawCircle(center.X, center.Y, radius, fillPaint);
        canvas.DrawCircle(center.X, center.Y, radius - 1.25f, borderPaint);
        canvas.DrawText($"+{hiddenCount}", center.X, center.Y + _overflowFont.Metrics.CapHeight / 2f,
            SKTextAlign.Center, _overflowFont, textPaint);
    }

    private static SKColor GetVoteColor(SongVoteLevel level) => level switch
    {
        SongVoteLevel.Love => new SKColor(242, 102, 146),
        SongVoteLevel.Like => new SKColor(91, 197, 132),
        SongVoteLevel.Neutral => new SKColor(151, 158, 173),
        SongVoteLevel.Dislike => new SKColor(240, 153, 83),
        SongVoteLevel.Trash => new SKColor(224, 76, 79),
        _ => SKColors.White
    };

    private static string GetVoteLabel(SongVoteLevel level) => level switch
    {
        SongVoteLevel.Love => "LOVE",
        SongVoteLevel.Like => "LIKE",
        SongVoteLevel.Neutral => "OK",
        SongVoteLevel.Dislike => "NOPE",
        SongVoteLevel.Trash => "TRASH",
        _ => string.Empty
    };

    private static SKRect CoverRect(float sourceWidth, float sourceHeight, float targetWidth, float targetHeight)
    {
        var scale = Math.Max(targetWidth / sourceWidth, targetHeight / sourceHeight);
        var scaledWidth = sourceWidth * scale;
        var scaledHeight = sourceHeight * scale;
        var left = (targetWidth - scaledWidth) / 2f;
        var top = (targetHeight - scaledHeight) / 2f;
        return new SKRect(left, top, left + scaledWidth, top + scaledHeight);
    }

    private static SKRect CenterCropRect(float sourceWidth, float sourceHeight)
    {
        var cropSize = Math.Min(sourceWidth, sourceHeight);
        var left = (sourceWidth - cropSize) / 2f;
        var top = (sourceHeight - cropSize) / 2f;
        return new SKRect(left, top, left + cropSize, top + cropSize);
    }

    private static SKRect Offset(SKRect rect, float x, float y)
        => new(rect.Left + x, rect.Top + y, rect.Right + x, rect.Bottom + y);

    private static List<string> WrapText(string text, SKFont font, float maxWidth, int maxLines)
    {
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var lines = new List<string>();
        var current = string.Empty;
        var truncated = false;

        foreach (var word in words)
        {
            var candidate = string.IsNullOrEmpty(current) ? word : $"{current} {word}";
            if (font.MeasureText(candidate) <= maxWidth)
            {
                current = candidate;
                continue;
            }

            if (!string.IsNullOrEmpty(current))
            {
                lines.Add(current);
                if (lines.Count == maxLines)
                {
                    truncated = true;
                    current = string.Empty;
                    break;
                }
            }

            current = word;

            if (font.MeasureText(current) <= maxWidth) continue;

            truncated = true;
            while (current.Length > 0 && font.MeasureText(current + "…") > maxWidth) current = current[..^1];
            lines.Add(current.TrimEnd() + "…");
            current = string.Empty;
            break;
        }

        if (!string.IsNullOrEmpty(current) && lines.Count < maxLines) lines.Add(current);
        if (lines.Count == 0) lines.Add(string.Empty);

        var consumedWords = string.Join(" ", lines).Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        truncated |= consumedWords < words.Length;
        if (!truncated || lines[^1].EndsWith('…')) return lines;

        var last = lines[^1];
        while (last.Length > 0 && font.MeasureText(last + "…") > maxWidth) last = last[..^1];
        lines[^1] = last.TrimEnd() + "…";
        return lines;
    }

    private static float Normalize(float value, float start, float end)
        => Math.Clamp((value - start) / (end - start), 0f, 1f);

    private static byte Alpha(float progress)
        => (byte)(255f * Math.Clamp(progress, 0f, 1f));

    private static float EaseOutCubic(float value)
        => 1f - MathF.Pow(1f - value, 3f);

    private static float EaseInOutCubic(float value) => value < 0.5f
        ? 4f * value * value * value
        : 1f - MathF.Pow(-2f * value + 2f, 3f) / 2f;
}
