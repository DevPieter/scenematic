using Scenematic.Worker.CoverGenerator.Rendering.Utils;

namespace Scenematic.Worker.CoverGenerator.Rendering.Scenes;

using Scenematic.Common.Enums.Songs;
using Common.Extensions;
using Scenematic.Data.Models.DTOs.Cover;
using CoverGenerator;
using Models;
using Rendering;
using SkiaSharp;

public class VotesRenderer : ISceneRenderer
{
    private const float Padding = 28f;
    private const float AvatarSize = 72f;
    private const float AvatarStep = 44f;
    private const float RailHeight = 16f;

    private static readonly SKColor SurfaceColor = new(16, 19, 27, 224);
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

    private readonly List<CoverVoteDto> _votes;
    private readonly SKImage _coverImage;
    private readonly Dictionary<string, SKImage> _avatarImages;
    private readonly SKSamplingOptions _sampling = new(SKCubicResampler.Mitchell);
    private readonly SKTypeface _boldTypeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold);
    private readonly SKFont _metaFont;
    private readonly SKFont _labelFont;
    private readonly SKFont _overflowFont;
    private readonly SKPaint _backgroundPaint = new() { IsAntialias = true };
    private readonly SKPaint _surfacePaint = new() { IsAntialias = true };

    private readonly SKPaint _borderPaint = new()
    {
        IsAntialias = true,
        Style = SKPaintStyle.Stroke,
        StrokeWidth = 1f
    };

    private readonly SKPaint _textPaint = new() { IsAntialias = true };

    private SKSurface? _surface;
    private SKRect _backgroundRect;
    private SKRect _cardRect;

    public VotesRenderer(
        List<CoverVoteDto> votes,
        SKImage coverImage,
        Dictionary<string, SKImage> avatarImages
    )
    {
        _votes = votes;
        _coverImage = coverImage;
        _avatarImages = avatarImages;
        _metaFont = new SKFont(_boldTypeface, 17f);
        _labelFont = new SKFont(_boldTypeface, 17f);
        _overflowFont = new SKFont(_boldTypeface, 17f);
        _backgroundPaint.ImageFilter = SKImageFilter.CreateBlur(24f, 24f);
        _backgroundPaint.Color = new SKColor(255, 255, 255, 190);
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
        _cardRect = new SKRect(Padding, Padding, context.Width - Padding, context.Height - Padding);
    }

    public byte[] Render(RenderContext context)
    {
        var canvas = _surface!.Canvas;
        canvas.Clear(new SKColor(8, 10, 15));
        canvas.DrawImage(_coverImage, _backgroundRect, _sampling, _backgroundPaint);
        using (var shade = new SKPaint { Color = new SKColor(7, 9, 14, 218) })
            canvas.DrawRect(0f, 0f, context.Width, context.Height, shade);

        var progress = DrawingUtils.EaseOutCubic(
            DrawingUtils.Normalize(context.SceneProgress, 0f, 0.22f)
        );
        DrawVoteCard(canvas, context, progress);

        using var image = _surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Webp, Renderer.Quality);
        return data.ToArray();
    }

    public void Dispose()
    {
        _surface?.Dispose();
        _boldTypeface.Dispose();
        _metaFont.Dispose();
        _labelFont.Dispose();
        _overflowFont.Dispose();
        _backgroundPaint.Dispose();
        _surfacePaint.Dispose();
        _borderPaint.Dispose();
        _textPaint.Dispose();
    }

    private void DrawVoteCard(SKCanvas canvas, RenderContext context, float progress)
    {
        var alpha = DrawingUtils.Alpha(progress);
        canvas.Save();
        canvas.Translate(0f, (1f - progress) * 10f);

        _surfacePaint.Color = SurfaceColor.WithAlpha((byte)(SurfaceColor.Alpha * progress));
        _borderPaint.Color = new SKColor(255, 255, 255, (byte)(30f * progress));
        canvas.DrawRoundRect(_cardRect, 22f, 22f, _surfacePaint);
        canvas.DrawRoundRect(_cardRect, 22f, 22f, _borderPaint);

        _textPaint.Color = new SKColor(164, 171, 185, alpha);
        canvas.DrawText(
            "COMMUNITY SCORE",
            _cardRect.Left + 26f,
            _cardRect.Top + 38f,
            SKTextAlign.Left,
            _metaFont,
            _textPaint
        );

        var winningLevel = _votes.Select(v => v.VoteLevel).ToList().CalculateLevel();
        var summary = _votes.Count == 0
            ? "NO VOTES YET"
            : $"{_votes.Count} {(_votes.Count == 1 ? "VOTE" : "VOTES")}  ·  {GetLabel(winningLevel)}";
        _textPaint.Color = PrimaryTextColor.WithAlpha(alpha);
        canvas.DrawText(
            summary,
            _cardRect.Right - 26f,
            _cardRect.Top + 38f,
            SKTextAlign.Right,
            _metaFont,
            _textPaint
        );

        var railRect = new SKRect(
            _cardRect.Left + 26f,
            _cardRect.Top + 94f,
            _cardRect.Right - 26f,
            _cardRect.Top + 94f + RailHeight
        );

        DrawRail(canvas, railRect, winningLevel, context.SceneProgress, progress);
        DrawAvatars(canvas, railRect, context.SceneProgress, progress);
        canvas.Restore();
    }

    private void DrawRail(
        SKCanvas canvas,
        SKRect railRect,
        SongVoteLevel winningLevel,
        float sceneProgress,
        float cardProgress
    )
    {
        var segmentWidth = railRect.Width / VoteLevels.Length;
        var highlight = DrawingUtils.EaseOutCubic(
            DrawingUtils.Normalize(sceneProgress, 0.18f, 0.48f)
        );

        for (var i = 0; i < VoteLevels.Length; i++)
        {
            var level = VoteLevels[i];
            var winner = _votes.Count > 0 && level == winningLevel;
            var color = GetColor(level);
            var segmentAlpha = (winner ? 90f + 165f * highlight : 48f) * cardProgress;
            using var segmentPaint = new SKPaint
            {
                Color = color.WithAlpha((byte)Math.Clamp(segmentAlpha, 0f, 255f)),
                IsAntialias = true
            };

            var segment = new SKRect(
                railRect.Left + i * segmentWidth + 3f,
                railRect.Top,
                railRect.Left + (i + 1) * segmentWidth - 3f,
                railRect.Bottom
            );
            canvas.DrawRoundRect(segment, RailHeight / 2f, RailHeight / 2f, segmentPaint);

            _textPaint.Color = (winner ? PrimaryTextColor : SecondaryTextColor)
                .WithAlpha((byte)((winner ? 245f : 145f) * cardProgress));
            canvas.DrawText(
                GetLabel(level),
                segment.MidX,
                railRect.Top - 17f,
                SKTextAlign.Center,
                _labelFont,
                _textPaint
            );
        }
    }

    private void DrawAvatars(SKCanvas canvas, SKRect railRect, float sceneProgress, float cardProgress)
    {
        var segmentWidth = railRect.Width / VoteLevels.Length;
        var maxVisible = Math.Max(1, (int)Math.Floor((segmentWidth - AvatarSize) / AvatarStep) + 1);
        var drawIndex = 0;

        for (var levelIndex = 0; levelIndex < VoteLevels.Length; levelIndex++)
        {
            var level = VoteLevels[levelIndex];
            var levelVotes = _votes.Where(v => v.VoteLevel == level).ToList();
            if (levelVotes.Count == 0) continue;

            var visible = levelVotes.Take(maxVisible).ToList();
            var hiddenCount = levelVotes.Count - visible.Count;
            if (hiddenCount > 0 && visible.Count > 1)
            {
                visible.RemoveAt(visible.Count - 1);
                hiddenCount++;
            }

            var chipCount = visible.Count + (hiddenCount > 0 ? 1 : 0);
            var totalWidth = AvatarSize + (chipCount - 1) * AvatarStep;
            var segmentCenterX = railRect.Left + levelIndex * segmentWidth + segmentWidth / 2f;
            var startX = segmentCenterX - totalWidth / 2f + AvatarSize / 2f;

            for (var i = 0; i < visible.Count; i++)
            {
                var entry = DrawingUtils.EaseOutCubic(
                    DrawingUtils.Normalize(sceneProgress, 0.24f + drawIndex * 0.025f, 0.56f)
                );
                DrawAvatar(
                    canvas,
                    visible[i],
                    new SKPoint(startX + i * AvatarStep, railRect.Bottom + 46f + (1f - entry) * 10f),
                    GetColor(level),
                    (byte)(255f * entry * cardProgress)
                );
                drawIndex++;
            }

            if (hiddenCount <= 0) continue;
            var overflowEntry = DrawingUtils.EaseOutCubic(
                DrawingUtils.Normalize(sceneProgress, 0.24f + drawIndex * 0.025f, 0.56f)
            );
            DrawOverflow(
                canvas,
                hiddenCount,
                new SKPoint(
                    startX + visible.Count * AvatarStep,
                    railRect.Bottom + 46f + (1f - overflowEntry) * 10f
                ),
                GetColor(level),
                (byte)(255f * overflowEntry * cardProgress)
            );
            drawIndex++;
        }
    }

    private void DrawAvatar(SKCanvas canvas, CoverVoteDto vote, SKPoint center, SKColor color, byte alpha)
    {
        if (!_avatarImages.TryGetValue(vote.AvatarUrl, out var avatar) || alpha == 0) return;
        var radius = AvatarSize / 2f;
        var rect = new SKRect(center.X - radius, center.Y - radius, center.X + radius, center.Y + radius);

        canvas.Save();
        using var path = new SKPath();
        path.AddCircle(center.X, center.Y, radius - 1f);
        canvas.ClipPath(path, antialias: true);
        using var imagePaint = new SKPaint { Color = SKColors.White.WithAlpha(alpha), IsAntialias = true };
        canvas.DrawImage(
            avatar,
            DrawingUtils.CenterCropRect(avatar.Width, avatar.Height),
            rect,
            _sampling,
            imagePaint
        );
        canvas.Restore();

        using var borderPaint = new SKPaint
        {
            Color = color.WithAlpha(alpha),
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 3f
        };
        canvas.DrawCircle(center.X, center.Y, radius - 1.5f, borderPaint);
    }

    private void DrawOverflow(SKCanvas canvas, int count, SKPoint center, SKColor color, byte alpha)
    {
        if (alpha == 0) return;
        var radius = AvatarSize / 2f;
        using var fill = new SKPaint
        {
            Color = new SKColor(20, 23, 31, (byte)(235f * alpha / 255f)),
            IsAntialias = true
        };
        using var border = new SKPaint
        {
            Color = color.WithAlpha(alpha),
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 3f
        };
        using var textPaint = new SKPaint { Color = PrimaryTextColor.WithAlpha(alpha), IsAntialias = true };

        canvas.DrawCircle(center.X, center.Y, radius, fill);
        canvas.DrawCircle(center.X, center.Y, radius - 1.5f, border);
        canvas.DrawText(
            $"+{count}",
            center.X,
            center.Y + _overflowFont.Metrics.CapHeight / 2f,
            SKTextAlign.Center,
            _overflowFont,
            textPaint
        );
    }

    private static SKColor GetColor(SongVoteLevel level) => level switch
    {
        SongVoteLevel.Love => new SKColor(242, 102, 146),
        SongVoteLevel.Like => new SKColor(91, 197, 132),
        SongVoteLevel.Neutral => new SKColor(151, 158, 173),
        SongVoteLevel.Dislike => new SKColor(240, 153, 83),
        SongVoteLevel.Trash => new SKColor(224, 76, 79),
        _ => SKColors.White
    };

    private static string GetLabel(SongVoteLevel level) => level switch
    {
        SongVoteLevel.Love => "LOVE",
        SongVoteLevel.Like => "LIKE",
        SongVoteLevel.Neutral => "OK",
        SongVoteLevel.Dislike => "NOPE",
        SongVoteLevel.Trash => "TRASH",
        _ => string.Empty
    };
}