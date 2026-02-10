namespace MusicGen.Core.Config;

public enum ScaleType
{
    Major,
    Minor,
}

public class SongConfig
{
    public int Seed { get; init; }

    public int Bpm { get; init; } = 240;
    public int BeatsPerBar { get; init; } = 4;

    public string Key { get; init; } = "C";
    public ScaleType Scale { get; init; } = ScaleType.Major;

    public int Bars { get; init; } = 12;

    public double NoteDensity { get; init; } = 0.5;
    public double RestProbability { get; init; } = 0.0;
    public double SwingAmount { get; init; } = 0.0;
    public double SyncopationChance { get; init; } = 0.0;
    public bool LoopFriendly { get; init; } = false;
    public InstrumentType Instrument { get; init; } = InstrumentType.Piano;
}
