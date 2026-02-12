using System;
using System.Collections.Generic;
using System.Linq;
using MusicGen.Core.Config;

namespace MusicGen.Core.Piano;

public record MelodyNote(int StartTick, int DurationTick, int Pitch, int Velocity = 80);

public class PianoGenerator
{
    private const int OctaveShift = 12;

    private int _scale;
    private List<List<int>> _minorChords;
    private List<int> _correctNotes;
    private List<List<int>> _baselines;
    private List<List<int>> _additionalChords;

    private const int TicksPerQuarter = 96;
    private readonly int[] _noteDuration =
    {
        4 * TicksPerQuarter,
        2 * TicksPerQuarter,
        1 * TicksPerQuarter,
        (int)(0.66 * TicksPerQuarter),
    };
    private readonly int[] _numberOfNotes = { 2, 2, 8, 12 };
    private readonly int[] _volumes = { 100, 50, 60, 60, 70, 80, 100, 80, 70, 60, 50, 50 };

    private Random _random;

    public PianoGenerator(int seed)
    {
        _random = new Random(seed);
    }

    public List<MelodyNote> Generate(SongConfig config)
    {
        int scale = MapKeyToMidiTuple(config.Key);
        InitializeMinorGenerator(scale);
        var melody = new List<MelodyNote>();

        double playChance = Math.Clamp(config.NoteDensity * 1.6, 0.2, 1.0);

        int intervals = config.Bars;

        for (int i = 0; i < intervals; i++)
        {
            int currentIndex = 4 * TicksPerQuarter * i;

            AddOneInterval(
                melody,
                currentIndex,
                playChance,
                rightHandShift: _random.Next(-1, 2),
                currentVelocity: _random.Next(80, 111),
                leftHandShift: _random.Next(-3, 0)
            );
        }

        AddOneInterval(melody, 4 * TicksPerQuarter * intervals, playChance, currentVelocity: 50);

        return melody;
    }

    private int MapKeyToMidiTuple(string key)
    {
        return key.ToUpper() switch
        {
            "C" => 60,
            "C#" or "DB" => 61,
            "D" => 62,
            "D#" or "EB" => 63,
            "E" => 64,
            "F" => 65,
            "F#" or "GB" => 66,
            "G" => 67,
            "G#" or "AB" => 68,
            "A" => 69,
            "A#" or "BB" => 70,
            "B" => 71,
            _ => 60,
        };
    }

    private void AddOneInterval(
        List<MelodyNote> melody,
        int intervalStartTick,
        double playChance,
        int rightHandShift = 0,
        int currentVelocity = 90,
        int leftHandShift = 0
    )
    {
        int currentIndexRight = intervalStartTick;
        int durationIndex = _random.Next(0, _noteDuration.Length);
        int currentNumberOfNotes = _numberOfNotes[durationIndex];
        int currentDuration = _noteDuration[durationIndex];
        int shift = rightHandShift * OctaveShift;

        for (int i = 0; i < currentNumberOfNotes; i++)
        {
            if (_random.NextDouble() < playChance)
            {
                int randomNoteIndex = _random.Next(0, 7); // 0 to 6
                int pitch = _correctNotes[randomNoteIndex] + shift;

                int noteDuration = currentDuration + TicksPerQuarter;

                melody.Add(new MelodyNote(currentIndexRight, noteDuration, pitch, currentVelocity));
            }
            currentIndexRight += currentDuration;
        }

        var sequence = _baselines[_random.Next(0, 3)];
        double currentIndexLeft = intervalStartTick;
        int tripletDuration = TicksPerQuarter / 3;

        for (int i = 0; i < 12; i++)
        {
            int noteVal = sequence[i];
            if (_random.NextDouble() < playChance)
            {
                int pitch = noteVal;

                melody.Add(
                    new MelodyNote((int)currentIndexLeft, TicksPerQuarter, pitch, _volumes[i])
                );
            }
            currentIndexLeft += tripletDuration;
        }
    }

    private void InitializeMinorGenerator(int scale)
    {
        if (scale < 59 || scale > 70)
            throw new ArgumentException("Scale must be 59-70", nameof(scale));
        _scale = scale;

        CorrectMinorChords();
        CreateBaselines();
        CalculateCorrectNotes();
        AddAdditionalChords();
    }

    private void CalculateCorrectNotes()
    {
        int[] shifts = { 0, 2, 3, 5, 7, 8, 10 };
        _correctNotes = shifts.Select(s => _scale + s).ToList();
    }

    private List<int> GetMinorChord(int note)
    {
        return new List<int> { note, note + 3, note + 7 };
    }

    private void CorrectMinorChords()
    {
        var first = GetMinorChord(_scale - 12);
        var second = GetMinorChord(_scale + 5 - 12);
        var third = GetMinorChord(_scale + 7 - 12);
        _minorChords = new List<List<int>> { first, second, third };
    }

    private void AddAdditionalChords()
    {
        var c1 = new List<int> { _scale, _scale + 3, _scale + 7, _scale + 8 };
        var c2 = new List<int> { _scale - 2, _scale + 2, _scale + 5, _scale + 8 };
        var c3 = new List<int> { _scale + 2, _scale + 5, _scale + 8, _scale + 12 };
        var c4 = new List<int> { _scale + 2, _scale + 5, _scale + 7 };
        var c5 = new List<int> { _scale, _scale + 3, _scale + 5 };

        _additionalChords = new List<List<int>> { c1, c2, c3, c4, c5 };
    }

    private List<int> CreateOneBaseline(int scale)
    {
        int cur = scale - 24;
        return new List<int>
        {
            cur,
            cur + 3,
            cur + 7,
            cur + 12,
            cur + 15,
            cur + 19,
            cur + 24,
            cur + 19,
            cur + 15,
            cur + 12,
            cur + 7,
            cur + 3,
        };
    }

    private void CreateBaselines()
    {
        var b1 = CreateOneBaseline(_scale);
        var b2 = CreateOneBaseline(_scale + 5);
        var b3 = CreateOneBaseline(_scale + 7);
        _baselines = new List<List<int>> { b1, b2, b3 };
    }
}
