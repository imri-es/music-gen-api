using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using MusicGen.Core.Config;
using MusicGen.Core.Piano;

namespace MusicGen.Core.Midi;

internal record TimedMidiEvent(long Time, MidiEvent Event);

public class MidiExporter
{
    public void Export(
        SongConfig config,
        List<MelodyNote> melody,
        string filePath,
        MusicDataDto? metadata = null
    )
    {
        var track = new TrackChunk();
        var events = new List<TimedMidiEvent>();

        events.Add(new TimedMidiEvent(0, new TimeSignatureEvent(4, 4)));

        var tempo = Tempo.FromBeatsPerMinute(config.Bpm);
        events.Add(new TimedMidiEvent(0, new SetTempoEvent(tempo.MicrosecondsPerQuarterNote)));

        events.Add(
            new TimedMidiEvent(0, new ProgramChangeEvent((SevenBitNumber)(int)config.Instrument))
        );

        if (metadata != null)
        {
            Console.WriteLine(
                $"[MidiExporter] Processing Metadata: {metadata.MusicName} by {metadata.ArtistName}"
            );

            if (!string.IsNullOrEmpty(metadata.MusicName))
            {
                Console.WriteLine(
                    $"[MidiExporter] Adding SequenceTrackNameEvent: {metadata.MusicName}"
                );
                events.Add(new TimedMidiEvent(0, new SequenceTrackNameEvent(metadata.MusicName)));
            }

            if (!string.IsNullOrEmpty(metadata.ArtistName))
            {
                Console.WriteLine($"[MidiExporter] Adding Artist TextEvent: {metadata.ArtistName}");
                events.Add(new TimedMidiEvent(0, new TextEvent($"Artist: {metadata.ArtistName}")));
            }

            if (!string.IsNullOrEmpty(metadata.AlbumTitle))
            {
                Console.WriteLine($"[MidiExporter] Adding Album TextEvent: {metadata.AlbumTitle}");
                events.Add(new TimedMidiEvent(0, new TextEvent($"Album: {metadata.AlbumTitle}")));
            }
        }

        int syllableIndex = 0;

        foreach (var note in melody)
        {
            var noteOn = new NoteOnEvent((SevenBitNumber)note.Pitch, (SevenBitNumber)note.Velocity);

            events.Add(new TimedMidiEvent(note.StartTick, noteOn));
            events.Add(
                new TimedMidiEvent(
                    note.StartTick + note.DurationTick,
                    new NoteOffEvent((SevenBitNumber)note.Pitch, (SevenBitNumber)0)
                )
            );
        }

        var ordered = events.OrderBy(e => e.Time).ToList();

        long lastTime = 0;
        foreach (var e in ordered)
        {
            e.Event.DeltaTime = e.Time - lastTime;
            lastTime = e.Time;
            track.Events.Add(e.Event);
        }

        var midiFile = new MidiFile(track);
        midiFile.TimeDivision = new TicksPerQuarterNoteTimeDivision(96);
        midiFile.Write(filePath, overwriteFile: true);
    }
}
