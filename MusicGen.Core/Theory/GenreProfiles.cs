namespace MusicGen.Core.Theory;

public static class GenreProfiles
{
    public static GenreProfile Classical =>
        new()
        {
            MinBpm = 60,
            MaxBpm = 120,
            NoteDensity = 0.4,
            RestProbability = 0.1,
            SwingAmount = 0.0,
            SyncopationChance = 0.1,
            LoopFriendly = false,
            Name = "Classical",
            Instrument = MusicGen.Core.Config.InstrumentType.Piano,
        };

    public static GenreProfile Rap =>
        new()
        {
            MinBpm = 70,
            MaxBpm = 100,
            NoteDensity = 0.6,
            RestProbability = 0.4,
            SwingAmount = 0.0,
            SyncopationChance = 0.3,
            LoopFriendly = true,
            Name = "Rap",
            Instrument = MusicGen.Core.Config.InstrumentType.ElectricGuitar,
        };

    public static GenreProfile Jazz =>
        new()
        {
            MinBpm = 90,
            MaxBpm = 140,
            NoteDensity = 0.7,
            RestProbability = 0.1,
            SwingAmount = 0.6,
            SyncopationChance = 0.6,
            LoopFriendly = false,
            Name = "Jazz",
            Instrument = MusicGen.Core.Config.InstrumentType.Drum,
        };

    public static GenreProfile HipHop =>
        new()
        {
            MinBpm = 70,
            MaxBpm = 95,
            NoteDensity = 0.5,
            RestProbability = 0.3,
            SwingAmount = 0.05,
            SyncopationChance = 0.4,
            LoopFriendly = true,
            Name = "HipHop",
            Instrument = MusicGen.Core.Config.InstrumentType.Bass,
        };

    public static GenreProfile GetProfile(int index) =>
        index switch
        {
            0 => Classical,
            1 => Rap,
            2 => Jazz,
            3 => HipHop,
            _ => Classical,
        };
}
