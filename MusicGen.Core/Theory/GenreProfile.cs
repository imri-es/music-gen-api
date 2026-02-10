namespace MusicGen.Core.Theory;

public class GenreProfile
{
    public int MinBpm { get; init; }
    public int MaxBpm { get; init; }
    public double NoteDensity { get; init; }
    public double RestProbability { get; init; }
    public double SwingAmount { get; init; }
    public double SyncopationChance { get; init; }
    public bool LoopFriendly { get; init; }
    public string Name { get; init; }
    public MusicGen.Core.Config.InstrumentType Instrument { get; init; }
}
