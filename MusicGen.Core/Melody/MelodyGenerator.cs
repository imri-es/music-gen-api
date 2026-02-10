using MusicGen.Core.Common;
using MusicGen.Core.Config;
using MusicGen.Core.Rhythm;
using MusicGen.Core.Theory;

namespace MusicGen.Core.Melody;

public class MelodyGenerator
{
    private readonly RandomProvider _random;

    public MelodyGenerator(RandomProvider random)
    {
        _random = random;
    }

    public List<MelodyNote> Generate(
        SongConfig config,
        List<RhythmNote> rhythm
    )
    {
        var melody = new List<MelodyNote>();

        int rootNote = 60; // C4
        int[] scale = config.Scale == ScaleType.Major
            ? Scale.Major
            : Scale.Minor;

        int currentPitch = rootNote;

        foreach (var r in rhythm)
        {
            // шаг вверх / вниз / стоим
            int step = _random.Next(-2, 3); // -2..+2

            int scaleIndex = Array.IndexOf(scale, (currentPitch - rootNote + 12) % 12);
            if (scaleIndex < 0)
                scaleIndex = 0;

            scaleIndex = Math.Clamp(scaleIndex + step, 0, scale.Length - 1);
            currentPitch = rootNote + scale[scaleIndex];

            melody.Add(new MelodyNote(
                StartTick: r.StartTick,
                DurationTick: r.DurationTick,
                Pitch: currentPitch
            ));
        }

        return melody;
    }
}
