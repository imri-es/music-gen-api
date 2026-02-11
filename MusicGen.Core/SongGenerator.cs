using Microsoft.Extensions.Configuration;
using MusicGen.Core.Common;
using MusicGen.Core.Config;
using MusicGen.Core.Images;
using MusicGen.Core.Melody;
using MusicGen.Core.Midi;
using MusicGen.Core.Rhythm;
using MusicGen.Core.Theory;

namespace MusicGen.Core;

public class SongGenerator
{
    private readonly string _apiKey;

    public SongGenerator(IConfiguration config)
    {
        _apiKey = config["GeminiApiKey"];
    }

    static readonly string OutputDir = Path.Combine("wwwroot", "songs");

    static SongGenerator()
    {
        Directory.CreateDirectory(OutputDir);
    }

    public async Task<SongResult> GenerateAsync(long seed, string language = "en")
    {
        int s32 = ToSeed32(seed);
        var data = new MusicDataGenerator().GenerateOne(seed, language);
        var (cfg, profile) = BuildConfig(s32);
        var (mel, lyr, img) = await Phase1(seed, s32, cfg, data);
        var paths = ExportMidi(seed, profile, cfg, mel, data);
        var vocalDuration = await RenderAudio(seed, cfg, lyr, paths, data, _apiKey);
        return Result(seed, profile, paths, img, lyr, vocalDuration);
    }

    static int ToSeed32(long s) => (int)(s ^ (s >> 32));

    static (SongConfig, GenreProfile) BuildConfig(int s32)
    {
        var r = new RandomProvider(s32);
        var p = GenreProfiles.GetProfile(r.Genre(4));
        return (Config(r, p, s32), p);
    }

    static SongConfig Config(RandomProvider r, GenreProfile p, int s) =>
        new()
        {
            Seed = s,
            Bpm = r.Next(p.MinBpm, p.MaxBpm),
            BeatsPerBar = 8,
            Key = "C",
            Scale = ScaleType.Major,
            Bars = 12,
            NoteDensity = p.NoteDensity,
            RestProbability = p.RestProbability,
            SwingAmount = p.SwingAmount,
            SyncopationChance = p.SyncopationChance,
            LoopFriendly = p.LoopFriendly,
            Instrument = p.Instrument,
        };

    static async Task<(object mel, object lyr, string img)> Phase1(
        long s,
        int s32,
        SongConfig c,
        object d
    )
    {
        var m = Task.Run(() => new Piano.PianoGenerator(s32).Generate(c));
        var l = Task.Run(() => new Lyrics.LyricsGenerator(s32).GenerateShortSong());
        var i = Image(s, d);
        await Task.WhenAll(m, l, i);
        return (m.Result!, l.Result!, i.Result);
    }

    static Task<string> Image(long s, object d) =>
        SeededImageService
            .GenerateAndSaveImageAsync(
                s,
                d.GetType().GetProperty("MusicName")?.GetValue(d)?.ToString()
                    + " "
                    + d.GetType().GetProperty("ArtistName")?.GetValue(d)?.ToString()
                    ?? "Unknown",
                Path.Combine(OutputDir, $"{s}.jpg")
            )
            .ContinueWith(_ => $"{s}.jpg");

    static (string midi, string beat, string final) ExportMidi(
        long s,
        GenreProfile p,
        SongConfig c,
        object m,
        object d
    )
    {
        var b = $"{s}{p.Name}";
        var midiPath = Path.Combine(OutputDir, $"{b}.mid");
        new MidiExporter().Export(c, (dynamic)m, midiPath, (dynamic)d);
        return ($"{b}.mid", $"{b}_beat.wav", $"{b}_song.wav");
    }

    static async Task<double> RenderAudio(
        long s,
        SongConfig c,
        object l,
        (string midi, string beat, string final) p,
        object d,
        string apiKey
    )
    {
        var r = new Audio.AudioRenderer(apiKey);
        var beatPath = Path.Combine(OutputDir, p.beat);
        var finalPath = Path.Combine(OutputDir, p.final);
        var midiPath = Path.Combine(OutputDir, p.midi);

        var beat = Task.Run(() => r.RenderInstrumental(c, midiPath, beatPath));
        var vox = r.FetchGeminiVocals(Lyrics(l), Voice(s));
        await Task.WhenAll(beat, vox);

        var (vocalBytes, duration) = vox.Result;

        if (vocalBytes?.Length > 0)
            r.MixAudio(beatPath, vocalBytes, finalPath, (dynamic)d);

        return duration;
    }

    static string Lyrics(object l)
    {
        var r = new List<string>();
        r.AddRange(((dynamic)l).Verse1);
        r.AddRange(((dynamic)l).Chorus);
        r.AddRange(((dynamic)l).Outro);
        return string.Join(Environment.NewLine, r);
    }

    static string Voice(long s) => new[] { "Aoede", "Puck", "Charon", "Fenrir" }[Math.Abs(s) % 4];

    static SongResult Result(
        long s,
        GenreProfile p,
        (string midi, string beat, string final) paths,
        string img,
        object lyricsObj,
        double vocalDuration
    )
    {
        var allLines = new List<string>();
        allLines.AddRange(((dynamic)lyricsObj).Verse1);
        allLines.AddRange(((dynamic)lyricsObj).Chorus);
        allLines.AddRange(((dynamic)lyricsObj).Outro);

        var lyricLines = new List<LyricLine>();
        if (allLines.Count > 0 && vocalDuration > 0)
        {
            double durationPerLine = vocalDuration / allLines.Count;
            for (int i = 0; i < allLines.Count; i++)
            {
                lyricLines.Add(
                    new LyricLine { Time = Math.Round(i * durationPerLine, 2), Text = allLines[i] }
                );
            }
        }

        return new()
        {
            Seed = s.ToString(),
            Genre = p.Name,
            MidiPath = $"/songs/{paths.midi}",
            InstrumentalPath = $"/songs/{paths.beat}",
            FinalMixPath = $"/songs/{paths.final}",
            ImagePath = $"/songs/{img}",
            ImageUrl = $"https://picsum.photos/seed/{s}/300/300",
            Lyrics = lyricLines,
        };
    }
}
