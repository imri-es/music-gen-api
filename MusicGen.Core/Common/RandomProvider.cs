namespace MusicGen.Core.Common;

public class RandomProvider
{
    private readonly Random _random;

    public RandomProvider(int seed)
    {
        _random = new Random(seed);
    }

    public int Next(int min, int max) => _random.Next(min, max);

    public bool Chance(double probability)
        => _random.NextDouble() < probability;

    public int Genre(int genre) => _random.Next(0, genre);
}
