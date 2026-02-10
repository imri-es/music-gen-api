namespace MusicGen.Core.Melody;

public record MelodyNote(
    int StartTick,
    int DurationTick,
    int Pitch, // MIDI note number
    int Velocity = 80 // Default velocity
);
