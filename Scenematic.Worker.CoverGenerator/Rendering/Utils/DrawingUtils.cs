using SkiaSharp;

namespace Scenematic.Worker.CoverGenerator.Rendering.Utils;

public static class DrawingUtils
{
    public static SKRect CoverRect(float sourceWidth, float sourceHeight, float targetWidth, float targetHeight)
    {
        var scale = Math.Max(targetWidth / sourceWidth, targetHeight / sourceHeight);
        var width = sourceWidth * scale;
        var height = sourceHeight * scale;
        var left = (targetWidth - width) / 2f;
        var top = (targetHeight - height) / 2f;
        return new SKRect(left, top, left + width, top + height);
    }

    public static SKRect CenterCropRect(float width, float height)
    {
        var size = Math.Min(width, height);
        var left = (width - size) / 2f;
        var top = (height - size) / 2f;
        return new SKRect(left, top, left + size, top + size);
    }

    public static SKRect Offset(SKRect rect, float x, float y)
        => new(rect.Left + x, rect.Top + y, rect.Right + x, rect.Bottom + y);

    public static List<string> WrapText(string text, SKFont font, float maxWidth, int maxLines)
    {
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var lines = new List<string>();
        var current = string.Empty;

        foreach (var word in words)
        {
            var candidate = string.IsNullOrEmpty(current) ? word : $"{current} {word}";
            if (font.MeasureText(candidate) <= maxWidth)
            {
                current = candidate;
                continue;
            }

            if (!string.IsNullOrEmpty(current)) lines.Add(current);
            current = word;
            if (lines.Count < maxLines) continue;
            current = string.Empty;
            break;
        }

        if (!string.IsNullOrEmpty(current) && lines.Count < maxLines) lines.Add(current);
        if (lines.Count == 0) lines.Add(string.Empty);

        var consumed = string.Join(" ", lines).Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        if (consumed >= words.Length) return lines;

        var last = lines[^1];
        while (last.Length > 0 && font.MeasureText(last + "…") > maxWidth) last = last[..^1];
        lines[^1] = last.TrimEnd() + "…";
        return lines;
    }

    public static float Normalize(float value, float start, float end)
        => Math.Clamp((value - start) / (end - start), 0f, 1f);

    public static byte Alpha(float progress)
        => (byte)(255f * Math.Clamp(progress, 0f, 1f));

    public static float EaseOutCubic(float value)
        => 1f - MathF.Pow(1f - value, 3f);

    public static float EaseInOutCubic(float value) => value < 0.5f
        ? 4f * value * value * value
        : 1f - MathF.Pow(-2f * value + 2f, 3f) / 2f;
}