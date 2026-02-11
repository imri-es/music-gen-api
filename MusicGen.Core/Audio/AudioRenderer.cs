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

    private static MeltySynth.SoundFont _cachedSoundFont;

    public void RenderInstrumental(SongConfig config, string midiPath, string outputPath)
    {
        if (_cachedSoundFont == null)
        {
            string soundFontPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "TimGM6mb.sf2"
            );
            if (!File.Exists(soundFontPath))
            {
                throw new FileNotFoundException(
                    $"SoundFont not found at {soundFontPath}. Please ensure TimGM6mb.sf2 is downloaded."
                );
            }
            _cachedSoundFont = new MeltySynth.SoundFont(soundFontPath);
        }

        var synthesizer = new MeltySynth.Synthesizer(_cachedSoundFont, SampleRate);
        var midiFile = new MeltySynth.MidiFile(midiPath);
        var sequencer = new MeltySynth.MidiFileSequencer(synthesizer);

        sequencer.Play(midiFile, loop: false);

        TimeSpan duration = midiFile.Length;
        int totalSamples = (int)(duration.TotalSeconds * SampleRate) + SampleRate;

        float[] left = new float[totalSamples];
        float[] right = new float[totalSamples];

        sequencer.Render(left, right);

        using (var writer = new WaveFileWriter(outputPath, new WaveFormat(SampleRate, 16, 2)))
        {
            for (int i = 0; i < totalSamples; i++)
            {
                short sLeft = (short)(Math.Clamp(left[i], -1f, 1f) * short.MaxValue);
                short sRight = (short)(Math.Clamp(right[i], -1f, 1f) * short.MaxValue);

                writer.WriteByte((byte)(sLeft & 0xFF));
                writer.WriteByte((byte)((sLeft >> 8) & 0xFF));
                writer.WriteByte((byte)(sRight & 0xFF));
                writer.WriteByte((byte)((sRight >> 8) & 0xFF));
            }
        }
    }

    public async Task GenerateSongWithGeminiVocals(
        string beatPath,
        string lyrics,
        string outputFilePath
    )
    {
        var (vocalBytes, duration) = await FetchGeminiVocals(lyrics);

        if (vocalBytes == null || vocalBytes.Length == 0)
            return;

        try
        {
            MixAudio(beatPath, vocalBytes, outputFilePath);
        }
        catch (Exception)
        {
            File.WriteAllBytes(outputFilePath, vocalBytes);
        }
    }

    public async Task<(byte[] Audio, double Duration)> FetchGeminiVocals(
        string text,
        string voiceName = "Kore"
    )
    {
        using var client = new HttpClient();

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
            return (Array.Empty<byte>(), 0);
        }

        var responseJson = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(responseJson);

        try
        {
            string base64Audio = doc
                .RootElement.GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("inlineData")
                .GetProperty("data")
                .GetString();

            byte[] vocalBytes = Convert.FromBase64String(base64Audio);

            using var ms = new MemoryStream();
            using (var writer = new WaveFileWriter(ms, new WaveFormat(24000, 16, 1)))
            {
                writer.Write(vocalBytes, 0, vocalBytes.Length);
            }

            double duration = (double)vocalBytes.Length / 48000.0;

            return (ms.ToArray(), duration);
        }
        catch (Exception)
        {
            return (Array.Empty<byte>(), 0);
        }
    }

    public void MixAudio(
        string beatPath,
        byte[] vocalBytes,
        string outputPath,
        MusicDataDto? metadata = null
    )
    {
        using var beatWaveReader = new WaveFileReader(beatPath);
        var beatReader = beatWaveReader.ToSampleProvider();
        var beatFormat = beatReader.WaveFormat;

        using var vocalStream = new MemoryStream(vocalBytes);

        using var vocalReader = new WaveFileReader(vocalStream);

        ISampleProvider vocalProvider = vocalReader.ToSampleProvider();

        if (vocalProvider.WaveFormat.SampleRate != beatReader.WaveFormat.SampleRate)
        {
            vocalProvider = new WdlResamplingSampleProvider(
                vocalProvider,
                beatReader.WaveFormat.SampleRate
            );
        }

        if (vocalProvider.WaveFormat.Channels != beatReader.WaveFormat.Channels)
        {
            if (vocalProvider.WaveFormat.Channels == 1 && beatReader.WaveFormat.Channels == 2)
            {
                vocalProvider = vocalProvider.ToStereo();
            }
            else if (vocalProvider.WaveFormat.Channels == 2 && beatReader.WaveFormat.Channels == 1)
            {
                vocalProvider = vocalProvider.ToMono();
            }
        }

        var mixer = new MixingSampleProvider(beatReader.WaveFormat);

        mixer.AddMixerInput((ISampleProvider)beatReader);
        mixer.AddMixerInput(vocalProvider);

        try
        {
            WaveFileWriter.CreateWaveFile16(outputPath, mixer);

            if (metadata != null)
            {
                AddMetadataToWav(outputPath, metadata);
            }
        }
        catch (Exception)
        {
            throw;
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

                var listData = new List<byte>();

                if (!string.IsNullOrEmpty(metadata.MusicName))
                {
                    listData.AddRange(Encoding.ASCII.GetBytes("INAM"));
                    var bytes = Encoding.Default.GetBytes(metadata.MusicName + "\0");
                    if (bytes.Length % 2 != 0)
                    {
                        var l = listData.Count;
                        listData.AddRange(bytes);
                        listData.Add(0);
                    }
                    else
                        listData.AddRange(bytes);

                    var len = bytes.Length + (bytes.Length % 2);
                    listData.InsertRange(listData.Count - len, BitConverter.GetBytes(len));
                }

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
                    listData.InsertRange(0, Encoding.ASCII.GetBytes("INFO"));
                    bw.Write(Encoding.ASCII.GetBytes("LIST"));
                    bw.Write(listData.Count);
                    bw.Write(listData.ToArray());

                    fs.Seek(4, SeekOrigin.Begin);
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
