using MusicGen.Core.Common;
using MusicGen.Core.Config;
using MusicGen.Core.Theory;

namespace MusicGen.Core.Rhythm;

public class RhythmGenerator
{
    private readonly RandomProvider _random;

    public RhythmGenerator(RandomProvider random)
    {
        _random = random;
    }

    public List<RhythmNote> Generate(SongConfig config)
    {
        var notes = new List<RhythmNote>();
        int ticksPerBeat = 80;
        int ticksPerBar = ticksPerBeat * config.BeatsPerBar;

        int currentTick = 0;

        for (int bar = 0; bar < config.Bars; bar++)
        {
            for (int beat = 0; beat < config.BeatsPerBar; beat++)
            {
                // Решаем: ставим ноту или нет
                if (_random.Chance(config.NoteDensity))
                {
                    // Проверяем паузу
                    if (!_random.Chance(config.RestProbability))
                    {
                        int startTick = currentTick;
                        int duration = ticksPerBeat;

                        // Применяем синкопу (сдвиг ноты)
                        if (_random.Chance(config.SyncopationChance))
                        {
                            int maxShift = ticksPerBeat / 2; // сдвигаем до половины доли
                            int shift = _random.Next(-maxShift, maxShift);
                            int minStart = currentTick;
                            int maxStart = currentTick + ticksPerBeat - 1;
                            startTick = Math.Clamp(startTick + shift, minStart, maxStart);
                        }

                        // Применяем свинг (двигаем каждую вторую долю)
                        if (config.SwingAmount > 0 && beat % 2 == 1)
                        {
                            int swingShift = (int)(ticksPerBeat * config.SwingAmount);
                            startTick += swingShift;
                        }

                        notes.Add(new RhythmNote(StartTick: startTick, DurationTick: duration));
                    }
                }

                currentTick += ticksPerBeat;
            }
        }

        return notes;
    }
}
