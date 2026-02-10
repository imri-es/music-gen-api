using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using SkiaSharp;

namespace MusicGen.Core.Images;

public static class SeededImageService
{
    static readonly HttpClient Http = new();

    public static async Task GenerateAndSaveImageAsync(
        long seed,
        string text,
        string path,
        int w = 300,
        int h = 300
    )
    {
        var bytes = await Download(seed, w, h);
        if (bytes == null)
            return;
        using var img = Decode(bytes);
        using var blurred = ApplyBlur(img);
        DrawText(blurred, text);
        await Save(path, Encode(blurred));
    }

    static async Task<byte[]?> Download(long s, int w, int h) =>
        await Try(() => Http.GetByteArrayAsync(Url(s, w, h)));

    static string Url(long s, int w, int h) => $"https://picsum.photos/seed/{s}/{w}/{h}";

    static SKBitmap Decode(byte[] b) => SKBitmap.Decode(b) ?? throw new Exception("Decode failed");

    static SKBitmap ApplyBlur(SKBitmap src)
    {
        var bmp = new SKBitmap(src.Width, src.Height);
        using var c = new SKCanvas(bmp);
        c.DrawBitmap(src, 0, 0, BlurPaint());
        return bmp;
    }

    static void DrawText(SKBitmap bmp, string text)
    {
        using var c = new SKCanvas(bmp);
        var p = TextPaint(SKColors.White);
        DrawLines(c, p, text, bmp);
    }

    static void DrawLines(SKCanvas c, SKPaint p, string t, SKBitmap bmp)
    {
        var lines = Wrap(p, t, bmp.Width - 40);
        float y = CenterY(p, lines.Count, bmp.Height);
        foreach (var l in lines)
            DrawLine(c, p, l, bmp, ref y);
    }

    static void DrawLine(SKCanvas c, SKPaint p, string l, SKBitmap b, ref float y)
    {
        float x = (b.Width - p.MeasureText(l)) / 2;
        DrawShadow(c, l, x, y);
        c.DrawText(l, x, y, p);
        y += p.TextSize * 1.2f;
    }

    static void DrawShadow(SKCanvas c, string t, float x, float y) =>
        c.DrawText(t, x + 2, y + 2, ShadowPaint());

    static List<string> Wrap(SKPaint p, string t, float w)
    {
        var r = new List<string>();
        var l = "";
        foreach (var word in t.Split(' '))
            (l = Fit(p, r, l, word, w))?.ToString();
        if (l != "")
            r.Add(l);
        return r;
    }

    static string Fit(SKPaint p, List<string> r, string l, string w, float mw)
    {
        var t = l == "" ? w : $"{l} {w}";
        if (p.MeasureText(t) < mw)
            return t;
        if (l != "")
            r.Add(l);
        return w;
    }

    static float CenterY(SKPaint p, int lines, int h) =>
        (h - lines * p.TextSize * 1.2f) / 2 + p.TextSize;

    static SKPaint BlurPaint() => new() { ImageFilter = SKImageFilter.CreateBlur(15, 15) };

    static SKPaint TextPaint(SKColor c) =>
        new()
        {
            Color = c,
            TextSize = 42,
            IsAntialias = true,
            Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold),
        };

    static SKPaint ShadowPaint() => TextPaint(SKColors.Black.WithAlpha(150));

    static byte[] Encode(SKBitmap bmp)
    {
        using var img = SKImage.FromBitmap(bmp);
        using var d = img.Encode(SKEncodedImageFormat.Png, 100);
        return d.ToArray();
    }

    static async Task Save(string p, byte[] d) => await File.WriteAllBytesAsync(p, d);

    static async Task<T?> Try<T>(Func<Task<T>> f)
    {
        try
        {
            return await f();
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return default;
        }
    }
}
