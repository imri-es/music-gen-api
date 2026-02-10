using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json; // strictly for parsing JSON response
using System.Threading.Tasks;
using MusicGen.Core.Config;
using MusicGen.Core.Melody;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace MusicGen.Core.Audio;

public class AudioRenderer
{
    private readonly string _apiKey;
    private readonly string _apiUrl;

    public AudioRenderer(string apiKey = "AIzaSyAvosGtR4Sn2U67EbZcQ-8GSozTN-BF7CE") // Default for fallback, but should be injected
    {
        _apiKey = apiKey;
        _apiUrl =
            $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash-preview-tts:generateContent?key={_apiKey}";
    }

    private const int SampleRate = 44100;
    private const int TicksPerQuarter = 96;

    public void RenderInstrumental(SongConfig config, string midiPath, string outputPath)
    {
        Console.WriteLine("[Renderer] Rendering Audio using MeltySynth...");

        string soundFontPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TimGM6mb.sf2");
        // if (!File.Exists(soundFontPath))
        // {
        //     // Fallback to searching in project root if not in bin
        //     soundFontPath = Path.Combine(
        //         Directory
        //             .GetParent(AppDomain.CurrentDomain.BaseDirectory)
        //             .Parent.Parent.Parent.FullName,
        //         "MusicGen.Core",
        //         "TimGM6mb.sf2"
        //     );
        // }

        if (!File.Exists(soundFontPath))
        {
            Console.WriteLine($"[ERROR] SoundFont not found at: {soundFontPath}");
            throw new FileNotFoundException(
                $"SoundFont not found at {soundFontPath}. Please ensure TimGM6mb.sf2 is downloaded."
            );
        }

        // if (new FileInfo(soundFontPath).Length < 1024 * 1024)
        // {
        //     throw new InvalidDataException(
        //         $"SoundFont at {soundFontPath} is too small (<1MB). It is likely corrupt or an HTML file. Please delete it and re-download."
        //     );
        // }

        // 1. Load SoundFont and MIDI
        var soundFont = new MeltySynth.SoundFont(soundFontPath);
        var synthesizer = new MeltySynth.Synthesizer(soundFont, SampleRate);
        var midiFile = new MeltySynth.MidiFile(midiPath);
        var sequencer = new MeltySynth.MidiFileSequencer(synthesizer);

        sequencer.Play(midiFile, loop: false);

        // 2. Render to float buffer
        // Calculate duration based on MIDI length
        // MeltySynth MidiFile doesn't expose duration directly easily without parsing,
        // but we can just render until sequencer stops?
        // Actually MidiFileSequencer doesn't have a "HasFinished" property easily exposed in all versions.
        // Let's assume a reasonable duration or render in blocks until EndOfSong.

        // Estimate duration:
        TimeSpan duration = midiFile.Length;
        int totalSamples = (int)(duration.TotalSeconds * SampleRate) + SampleRate; // +1 sec buffer

        float[] left = new float[totalSamples];
        float[] right = new float[totalSamples];

        sequencer.Render(left, right);

        // 3. Write to WAV (16-bit)
        using (var writer = new WaveFileWriter(outputPath, new WaveFormat(SampleRate, 16, 2)))
        {
            Console.WriteLine($"totalSamples: {totalSamples}");
            for (int i = 0; i < totalSamples; i++)
            {
                // Interleave Stereo
                short sLeft = (short)(Math.Clamp(left[i], -1f, 1f) * short.MaxValue);
                short sRight = (short)(Math.Clamp(right[i], -1f, 1f) * short.MaxValue);

                writer.WriteByte((byte)(sLeft & 0xFF));
                writer.WriteByte((byte)((sLeft >> 8) & 0xFF));
                writer.WriteByte((byte)(sRight & 0xFF));
                writer.WriteByte((byte)((sRight >> 8) & 0xFF));
            }
        }
        Console.WriteLine($"[Renderer] Beat saved to: {outputPath}");
    }

    public async Task GenerateSongWithGeminiVocals(
        string beatPath,
        string lyrics,
        string outputFilePath
    )
    {
        Console.WriteLine("1. Requesting Vocals from Gemini...");

        // 1. Get Vocals
        byte[] vocalBytes = await FetchGeminiVocals(lyrics);

        if (vocalBytes == null || vocalBytes.Length == 0)
            return;

        // 3. If it's a real audio file (MP3/WAV), proceed to mix
        Console.WriteLine("2. Mixing Vocals with Audio Beat...");
        try
        {
            MixAudio(beatPath, vocalBytes, outputFilePath);
            Console.WriteLine($"Full song saved to: {outputFilePath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Mixing failed: {ex.Message}. Saving raw vocals instead.");
            File.WriteAllBytes(outputFilePath, vocalBytes);
        }
    }

    public async Task<byte[]> FetchGeminiVocals(string text, string voiceName = "Kore")
    {
        using var client = new HttpClient();

        // We ask Gemini to speak the text.
        // Note: We prompt it to be "rhythmic" or "dramatic" to fit music better.
        var payload = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new
                        {
                            text = $"Say the following lines with a rhythmic, deep, and energetic flow, suitable for a music track intro: \n\n{text}",
                        },
                    },
                },
            },
            // CRITICAL: Request AUDIO output
            generationConfig = new
            {
                responseModalities = new[] { "AUDIO" },
                speechConfig = new
                {
                    voiceConfig = new { prebuiltVoiceConfig = new { voiceName = voiceName } },
                },
            },
            model = "gemini-2.5-flash-preview-tts",
        };

        string jsonPayload = JsonSerializer.Serialize(payload);
        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        var response = await client.PostAsync(_apiUrl, content);

        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine(
                $"Error: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}"
            );
            return null;
        }

        var responseJson = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(responseJson);

        // Parse the Base64 audio from the JSON response
        // Structure: candidates[0].content.parts[0].inlineData.data
        try
        {
            // ... inside FetchGeminiVocals method ...

            // 1. Extract the Base64 string
            // (Note: Use the "TryGetProperty" logic from my previous answer if you get JSON errors again)
            string base64Audio = doc
                .RootElement.GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("inlineData") // or "inline_data" if using snake_case
                .GetProperty("data")
                .GetString();

            // 2. Convert to Bytes
            byte[] vocalBytes = Convert.FromBase64String(base64Audio);

            // Wrap raw PCM into WAV
            using (var writer = new WaveFileWriter("debug_fixed.wav", new WaveFormat(24000, 16, 1))) // Gemini usually outputs 24kHz Mono
            {
                writer.Write(vocalBytes, 0, vocalBytes.Length);
            }
            Console.WriteLine("[Debug] Wrapped raw PCM into 'debug_fixed.wav'");

            return File.ReadAllBytes("debug_fixed.wav");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Could not parse audio from response: " + ex.Message);
            return null;
        }
    }

    public void MixAudio(
        string beatPath,
        byte[] vocalBytes,
        string outputPath,
        MusicDataDto? metadata = null
    )
    {
        Console.WriteLine($"[Mixer] Starting audio mix...");
        Console.WriteLine($"[Mixer] Loading Beat: {Path.GetFileName(beatPath)}");

        // 1. Load the Beat (The Master Track)
        using var beatReader = new AudioFileReader(beatPath);
        var beatFormat = beatReader.WaveFormat;
        Console.WriteLine(
            $"[Mixer] Beat Format: {beatFormat.SampleRate}Hz, {beatFormat.Channels}ch, {beatFormat.BitsPerSample}bit"
        );

        // 2. Load the Vocals (The Raw Bytes from Gemini)
        Console.WriteLine($"[Mixer] Loading Vocals ({vocalBytes.Length} bytes)...");
        using var vocalStream = new MemoryStream(vocalBytes);

        // Use StreamMediaFoundationReader to sniff the header (WAV/MP3/PCM)
        using var vocalReader = new StreamMediaFoundationReader(vocalStream);
        Console.WriteLine(
            $"[Mixer] Vocal Raw Format: {vocalReader.WaveFormat.SampleRate}Hz, {vocalReader.WaveFormat.Channels}ch"
        );

        // 3. Convert Vocals to IEEE Float (ISampleProvider)
        ISampleProvider vocalProvider = vocalReader.ToSampleProvider();

        // 4. RESAMPLE Vocals to match the Beat
        if (vocalProvider.WaveFormat.SampleRate != beatReader.WaveFormat.SampleRate)
        {
            Console.WriteLine(
                $"[Mixer] Resampling Vocals: {vocalProvider.WaveFormat.SampleRate}Hz -> {beatReader.WaveFormat.SampleRate}Hz"
            );
            vocalProvider = new WdlResamplingSampleProvider(
                vocalProvider,
                beatReader.WaveFormat.SampleRate
            );
        }
        else
        {
            Console.WriteLine("[Mixer] Sample rates match. No resampling needed.");
        }

        // 5. MATCH CHANNELS (Stereo vs Mono)
        if (vocalProvider.WaveFormat.Channels != beatReader.WaveFormat.Channels)
        {
            Console.WriteLine(
                $"[Mixer] Converting Channels: {vocalProvider.WaveFormat.Channels}ch -> {beatReader.WaveFormat.Channels}ch"
            );

            if (vocalProvider.WaveFormat.Channels == 1 && beatReader.WaveFormat.Channels == 2)
            {
                vocalProvider = vocalProvider.ToStereo();
            }
            else if (vocalProvider.WaveFormat.Channels == 2 && beatReader.WaveFormat.Channels == 1)
            {
                vocalProvider = vocalProvider.ToMono();
            }
        }

        // 6. Create the Mixer
        Console.WriteLine("[Mixer] Initializing Mixing Engine...");
        var mixer = new MixingSampleProvider(beatReader.WaveFormat); // Master format

        // Add inputs
        mixer.AddMixerInput((ISampleProvider)beatReader);
        mixer.AddMixerInput(vocalProvider);

        // 7. Render to File
        Console.WriteLine($"[Mixer] Rendering to disk: {outputPath}...");
        try
        {
            WaveFileWriter.CreateWaveFile16(outputPath, mixer);
            Console.WriteLine("[Mixer] Success! File saved.");

            if (metadata != null)
            {
                Console.WriteLine("[Mixer] Adding Metadata to WAV...");
                AddMetadataToWav(outputPath, metadata);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Mixer] FATAL ERROR during write: {ex.Message}");
            throw; // Re-throw so the main loop knows it failed
        }
    }

    private void AddMetadataToWav(string filePath, MusicDataDto metadata)
    {
        try
        {
            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite))
            using (var bw = new BinaryWriter(fs))
            {
                fs.Seek(0, SeekOrigin.End);
                long originalLength = fs.Length;

                // Create LIST INFO chunk
                var listData = new List<byte>();

                // INAM (Title)
                if (!string.IsNullOrEmpty(metadata.MusicName))
                {
                    listData.AddRange(Encoding.ASCII.GetBytes("INAM"));
                    var bytes = Encoding.Default.GetBytes(metadata.MusicName + "\0");
                    if (bytes.Length % 2 != 0)
                    {
                        var l = listData.Count;
                        listData.AddRange(bytes);
                        listData.Add(0);
                    } // Word align
                    else
                        listData.AddRange(bytes);

                    // Insert Size before data
                    var len = bytes.Length + (bytes.Length % 2); // padded length
                    listData.InsertRange(listData.Count - len, BitConverter.GetBytes(len));
                }

                // IART (Artist)
                if (!string.IsNullOrEmpty(metadata.ArtistName))
                {
                    listData.AddRange(Encoding.ASCII.GetBytes("IART"));
                    var bytes = Encoding.Default.GetBytes(metadata.ArtistName + "\0");
                    var len = bytes.Length;
                    listData.AddRange(BitConverter.GetBytes(len + (len % 2)));
                    listData.AddRange(bytes);
                    if (len % 2 != 0)
                        listData.Add(0);
                }

                // IPRD (Album/Product)
                if (!string.IsNullOrEmpty(metadata.AlbumTitle))
                {
                    listData.AddRange(Encoding.ASCII.GetBytes("IPRD"));
                    var bytes = Encoding.Default.GetBytes(metadata.AlbumTitle + "\0");
                    var len = bytes.Length;
                    listData.AddRange(BitConverter.GetBytes(len + (len % 2)));
                    listData.AddRange(bytes);
                    if (len % 2 != 0)
                        listData.Add(0);
                }

                if (listData.Count > 0)
                {
                    // Write LIST chunk
                    listData.InsertRange(0, Encoding.ASCII.GetBytes("INFO"));
                    bw.Write(Encoding.ASCII.GetBytes("LIST"));
                    bw.Write(listData.Count);
                    bw.Write(listData.ToArray());

                    // Update RIFF Size
                    fs.Seek(4, SeekOrigin.Begin);
                    // RIFF size = FileSize - 8
                    // We added 'LIST' (4) + Size (4) + Data
                    // So new size is oldSize + 8 + listData.Count

                    // Or easier: total file size - 8
                    long newFileSize = originalLength + 8 + listData.Count;
                    bw.Write((int)(newFileSize - 8));

                    Console.WriteLine("[Mixer] Metadata added successfully.");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Mixer] Failed to add metadata: {ex.Message}");
        }
    }
}
