using System;
using System.Collections.Generic;

namespace MusicGen.Core.Lyrics;

public class LyricsGenerator
{
    private readonly Random _random;

    public LyricsGenerator(int seed)
    {
        _random = new Random(seed);
    }

    public SongLyrics GenerateShortSong()
    {
        return new SongLyrics(
            Verse1: GenerateLines(4),
            Chorus: GenerateLines(4),
            Outro: GenerateLines(2)
        );
    }

    public List<string> GenerateLines(int count)
    {
        var lines = new List<string>();

        for (int i = 0; i < count; i++)
        {
            lines.Add(GenerateLine());
        }

        return lines;
    }

    private string GenerateLine()
    {
        string template = LineTemplates.Templates[_random.Next(LineTemplates.Templates.Length)];

        return template
            .Replace("{adj}", Pick(WordBank.Adjectives))
            .Replace("{noun}", Pick(WordBank.Nouns))
            .Replace("{verb}", Pick(WordBank.Verbs));
    }

    private string Pick(string[] source)
    {
        return source[_random.Next(source.Length)];
    }
}

public record SongLyrics(List<string> Verse1, List<string> Chorus, List<string> Outro);
