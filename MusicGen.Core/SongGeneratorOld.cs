// using MusicGen.Core.Common;
// using MusicGen.Core.Config;
// using MusicGen.Core.Images;
// using MusicGen.Core.Melody;
// using MusicGen.Core.Midi;
// using MusicGen.Core.Rhythm;
// using MusicGen.Core.Theory;

// namespace MusicGen.Core;

// public class SongGenerator
// {
//     public async Task<SongResult> GenerateAsync(long seed)
//     {
//         int seed32 = (int)(seed ^ (seed >> 32));

//         var musicDataGenerator = new MusicDataGenerator();
//         var musicData = musicDataGenerator.GenerateOne(seed);

//         var random = new RandomProvider(seed32);
//         // 0=Classical, 1=Rap, 2=Jazz, 3=HipHop
//         int genreIndex = random.Genre(4);

//         var profile = GenreProfiles.GetProfile(genreIndex);

//         var config = new SongConfig
//         {
//             Seed = seed32,
//             Bpm = random.Next(profile.MinBpm, profile.MaxBpm),
//             BeatsPerBar = 8,
//             Key = "C",
//             Scale = ScaleType.Major,
//             Bars = 12,
//             NoteDensity = profile.NoteDensity,
//             RestProbability = profile.RestProbability,
//             SwingAmount = profile.SwingAmount,
//             SyncopationChance = profile.SyncopationChance,
//             LoopFriendly = profile.LoopFriendly,
//             Instrument = profile.Instrument,
//         };

//         string imagePath = $"{seed}_{profile.Name}.jpg";
//         string imageUrl = $"https://picsum.photos/seed/{seed}/300/300";

//         Console.WriteLine($"[SongGenerator] Starting Parallel Tasks for Seed: {seed}");

//         // parallel tasks
//         // melody generator
//         var melodyTask = Task.Run(() =>
//         {
//             var pianoGen = new Piano.PianoGenerator(seed32);
//             return pianoGen.Generate(config);
//         });

//         // lyrics gen
//         var lyricsTask = Task.Run(() =>
//         {
//             var lyricGen = new Lyrics.LyricsGenerator(seed32);
//             return lyricGen.GenerateShortSong();
//         });

//         // image download & processing
//         var imageTask = SeededImageService.GenerateAndSaveImageAsync(
//             seed,
//             musicData.MusicName ?? "Unknown Song",
//             imagePath
//         );

//         // Wait for Melody & Lyrics (needed for MIDI export) and Image
//         await Task.WhenAll(melodyTask, lyricsTask, imageTask);

//         var melody = melodyTask.Result;
//         var songLyrics = lyricsTask.Result;

//         // --- PARALLEL TASKS END (for Phase 1) ---

//         // Structure Lyrics
//         var allLines = new List<string>();
//         allLines.AddRange(songLyrics.Verse1);
//         allLines.AddRange(songLyrics.Chorus);
//         allLines.AddRange(songLyrics.Outro);

//         string fullLyrics = string.Join(Environment.NewLine, allLines);
//         var exporter = new MidiExporter();

//         // Export MIDI
//         string midiPath = $"{seed + profile.Name}.mid";
//         exporter.Export(config, melody, midiPath, musicData);

//         // Prepare Audio Rendering Paths
//         string beatWavPath = $"{seed + profile.Name}_beat.wav";
//         string finalWavPath = $"{seed + profile.Name}_song.wav";

//         // Select Voice
//         string[] voices = { "Aoede", "Puck", "Charon", "Fenrir" };
//         string selectedVoice = voices[Math.Abs(seed) % voices.Length];

//         // --- AUDIO RENDERING PARALLEL START ---

//         try
//         {
//             var audioRenderer = new Audio.AudioRenderer();

//             // Render Instrumental (CPU/FAST)
//             var beatTask = Task.Run(() =>
//                 audioRenderer.RenderInstrumental(config, midiPath, beatWavPath)
//             );

//             // Fetch Vocals (Network/SLOW)
//             var vocalTask = audioRenderer.FetchGeminiVocals(fullLyrics, selectedVoice);

//             await Task.WhenAll(beatTask, vocalTask);

//             var vocalBytes = vocalTask.Result;
//             string vocalPath = "";

//             if (vocalBytes != null && vocalBytes.Length > 0)
//             {
//                 // Optionally save raw vocals if needed, but here we just mix
//                 // For completeness, let's treat it as a stream or just mix
//                 audioRenderer.MixAudio(beatWavPath, vocalBytes, finalWavPath, musicData);
//             }
//             else
//             {
//                 Console.WriteLine("[SongGenerator] Vocals failed, skipping mix.");
//             }
//         }
//         catch (Exception ex)
//         {
//             Console.WriteLine($"Error generating audio: {ex.Message}");
//         }

//         return new SongResult
//         {
//             Seed = seed,
//             Genre = profile.Name,
//             MidiPath = Path.GetFullPath(midiPath),
//             InstrumentalPath = Path.GetFullPath(beatWavPath),
//             FinalMixPath = Path.GetFullPath(finalWavPath), // This might not exist if mix failed, but path is valid
//             ImagePath = Path.GetFullPath(imagePath),
//             ImageUrl = imageUrl,
//         };
//     }
// }
