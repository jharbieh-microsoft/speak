using System.Speech.Synthesis;
using System.Runtime.InteropServices;

// Simple option record
record Options(
    string? FilePath,
    string? Voice,
    int? Rate,
    int? Volume,
    string? SavePath,
    bool ListOnly,
    bool Help,
    bool UseAzure,
    string? AzureRegion,
    string? AzureKey,
    string? AzureFormat,
    string? SsmlPath
);

internal class Program
{
    private static void Main(string[] args)
    {
        var options = ParseArgs(args);
        if (options.Help)
        {
            PrintHelp();
            return;
        }

        if (options.UseAzure)
        {
            RunAzureAsync(options).GetAwaiter().GetResult();
            return;
        }

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Console.Error.WriteLine("Windows-only mode selected (System.Speech) but OS is not Windows. Use --azure instead.");
            return;
        }

        using var synth = new SpeechSynthesizer();
        var voices = synth.GetInstalledVoices().ToList();
        if (!voices.Any())
        {
            Console.Error.WriteLine("No installed voices were found.");
            return;
        }

        if (options.ListOnly)
        {
            ListVoices(voices);
            return;
        }

        // Select voice: precedence order -> explicit --voice -> interactive
        if (!TrySelectVoice(synth, voices, options.Voice))
        {
            // Interactive selection if no valid voice specified
            ListVoices(voices);
            Console.Write("Select a voice index: ");
            var input = Console.ReadLine();
            if (!TrySelectVoice(synth, voices, input))
            {
                Console.WriteLine("Defaulting to first voice: {0}", voices[0].VoiceInfo.Name);
                synth.SelectVoice(voices[0].VoiceInfo.Name);
            }
        }

        // Rate and Volume
        if (options.Rate is int rate)
        {
            synth.Rate = Math.Clamp(rate, -10, 10);
        }
        if (options.Volume is int volume)
        {
            synth.Volume = Math.Clamp(volume, 0, 100);
        }

        // Determine text source
        var fileToRead = options.FilePath ?? Path.Combine(Directory.GetCurrentDirectory(), "README.md");
        if (!File.Exists(fileToRead))
        {
            Console.Error.WriteLine($"File not found: {fileToRead}");
            return;
        }
        string text;
        try
        {
            text = File.ReadAllText(fileToRead);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to read file: {ex.Message}");
            return;
        }

        // If saving to wav requested
        if (!string.IsNullOrWhiteSpace(options.SavePath))
        {
            try
            {
                var savePath = Path.GetFullPath(options.SavePath);
                var dir = Path.GetDirectoryName(savePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);

                // First pass: write to wave file only
                synth.SetOutputToWaveFile(savePath);
                synth.Speak(text);
                synth.Speak("Audio has been written to the specified wave file.");
                synth.SetOutputToDefaultAudioDevice();
                Console.WriteLine($"Saved audio to {savePath}");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Failed to save WAV file: {ex.Message}");
            }
        }

        // Always speak to default device (second pass) unless user explicitly disabled? (Not implemented – always speak.)
        synth.SetOutputToDefaultAudioDevice();
        synth.Speak(text);
        synth.Speak("Sample complete.");
    }

    private static bool TrySelectVoice(SpeechSynthesizer synth, List<InstalledVoice> voices, string? selector)
    {
        if (string.IsNullOrWhiteSpace(selector)) return false;
        selector = selector.Trim();
        // Index?
        if (int.TryParse(selector, out var idx))
        {
            if (idx >= 0 && idx < voices.Count)
            {
                synth.SelectVoice(voices[idx].VoiceInfo.Name);
                return true;
            }
            return false;
        }
        // Name match (case-insensitive contains or equals)
        var match = voices.FirstOrDefault(v => v.VoiceInfo.Name.Equals(selector, StringComparison.OrdinalIgnoreCase))
                    ?? voices.FirstOrDefault(v => v.VoiceInfo.Name.Contains(selector, StringComparison.OrdinalIgnoreCase));
        if (match != null)
        {
            synth.SelectVoice(match.VoiceInfo.Name);
            return true;
        }
        return false;
    }

    private static void ListVoices(List<InstalledVoice> voices)
    {
        for (int i = 0; i < voices.Count; i++)
        {
            var info = voices[i].VoiceInfo;
            Console.WriteLine($"[{i}] {info.Name} | Culture={info.Culture} | Gender={info.Gender} | Age={info.Age}");
        }
    }

    private static Options ParseArgs(string[] args)
    {
        string? file = null; string? voice = null; int? rate = null; int? volume = null; string? save = null; bool list = false; bool help = false;
        bool useAzure = false; string? region = Environment.GetEnvironmentVariable("AZURE_SPEECH_REGION"); string? key = Environment.GetEnvironmentVariable("AZURE_SPEECH_KEY"); string? format = null; string? ssml = null;
        for (int i = 0; i < args.Length; i++)
        {
            var a = args[i];
            switch (a)
            {
                case "--file":
                case "-f":
                    if (i + 1 < args.Length) file = args[++i];
                    break;
                case "--voice":
                case "-v":
                    if (i + 1 < args.Length) voice = args[++i];
                    break;
                case "--rate":
                case "-r":
                    if (i + 1 < args.Length && int.TryParse(args[i + 1], out var r)) { rate = r; i++; }
                    break;
                case "--volume":
                case "--vol":
                    if (i + 1 < args.Length && int.TryParse(args[i + 1], out var vol)) { volume = vol; i++; }
                    break;
                case "--save":
                case "-s":
                    if (i + 1 < args.Length) save = args[++i];
                    break;
                case "--list":
                case "-l":
                    list = true;
                    break;
                case "--help":
                case "-h":
                case "-?":
                    help = true;
                    break;
                case "--azure":
                    useAzure = true;
                    break;
                case "--azure-region":
                    if (i + 1 < args.Length) region = args[++i];
                    break;
                case "--azure-key":
                    if (i + 1 < args.Length) key = args[++i];
                    break;
                case "--audio-format":
                    if (i + 1 < args.Length) format = args[++i];
                    break;
                case "--ssml":
                    if (i + 1 < args.Length) ssml = args[++i];
                    break;
            }
        }
        return new Options(file, voice, rate, volume, save, list, help, useAzure, region, key, format, ssml);
    }

    private static void PrintHelp()
    {
        Console.WriteLine(@"Usage: speak [options]

Options:
    -f, --file <path>      File to read (default: README.md in current directory)
    -v, --voice <name|idx> Voice name (full/partial, case-insensitive) or index
    -r, --rate <int>       Speaking rate (-10..10)
            --volume <0-100>   Output volume percentage (0..100)
    -s, --save <wav>       Save spoken text to specified WAV file (in addition to live playback)
    -l, --list             List installed voices and exit
    -h, --help             Show this help and exit

Examples:
    speak --list
    speak --voice 1 --file intro.txt
          speak --voice 1 --rate 2 --volume 80
          speak --voice ""Microsoft Zira"" --rate 2 --volume 80 --save out/readme.wav

        Azure (cross-platform) examples:
          speak --azure --file README.md --voice en-US-AriaNeural
          speak --azure --ssml aria.ssml --audio-format riff-24khz-16bit-mono-pcm
        Environment variables (fallback): AZURE_SPEECH_REGION, AZURE_SPEECH_KEY");
    }

            // Azure Speech integration
            private static async Task RunAzureAsync(Options options)
            {
                // Lazy load to avoid needing Microsoft.CognitiveServices.Speech on non-Azure path if trimmed
                var region = options.AzureRegion ?? Environment.GetEnvironmentVariable("AZURE_SPEECH_REGION");
                var key = options.AzureKey ?? Environment.GetEnvironmentVariable("AZURE_SPEECH_KEY");
                if (string.IsNullOrWhiteSpace(region) || string.IsNullOrWhiteSpace(key))
                {
                    Console.Error.WriteLine("Azure Speech requires region and key (--azure-region/--azure-key or env vars AZURE_SPEECH_REGION/AZURE_SPEECH_KEY).");
                    return;
                }
                string? text = null;
                if (!string.IsNullOrEmpty(options.SsmlPath))
                {
                    if (!File.Exists(options.SsmlPath)) { Console.Error.WriteLine("SSML file not found: " + options.SsmlPath); return; }
                    text = await File.ReadAllTextAsync(options.SsmlPath);
                }
                else
                {
                    var file = options.FilePath ?? Path.Combine(Directory.GetCurrentDirectory(), "README.md");
                    if (!File.Exists(file)) { Console.Error.WriteLine("File not found: " + file); return; }
                    text = await File.ReadAllTextAsync(file);
                }
                if (string.IsNullOrWhiteSpace(text)) { Console.Error.WriteLine("No input text."); return; }

                try
                {
                    var config = Microsoft.CognitiveServices.Speech.SpeechConfig.FromSubscription(key, region);
                    if (!string.IsNullOrWhiteSpace(options.Voice)) config.SpeechSynthesisVoiceName = options.Voice;
                    if (options.Rate is int r) config.SetProperty("SpeechSynthesis:Rate", r.ToString());
                    if (options.Volume is int v) config.SetProperty("SpeechSynthesis:Volume", v.ToString());
                    if (!string.IsNullOrWhiteSpace(options.AzureFormat)) config.SetSpeechSynthesisOutputFormat(ParseFormat(options.AzureFormat));

                    using var result = await SynthesizeAsync(config, text, isSsml: !string.IsNullOrEmpty(options.SsmlPath));
                    if (result.Reason == Microsoft.CognitiveServices.Speech.ResultReason.SynthesizingAudioCompleted)
                    {
                        Console.WriteLine("Synthesis completed. Audio length: {0} bytes", result.AudioData.Length);
                        if (!string.IsNullOrWhiteSpace(options.SavePath))
                        {
                            var path = Path.GetFullPath(options.SavePath!);
                            var dir = Path.GetDirectoryName(path);
                            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
                            await File.WriteAllBytesAsync(path, result.AudioData);
                            Console.WriteLine("Saved: " + path);
                        }
                    }
                    else if (result.Reason == Microsoft.CognitiveServices.Speech.ResultReason.Canceled)
                    {
                        var details = Microsoft.CognitiveServices.Speech.CancellationDetails.FromResult(result);
                        Console.Error.WriteLine("Synthesis canceled: " + details.Reason + " | " + details.ErrorDetails);
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine("Azure synthesis failed: " + ex.Message);
                }
            }

            private static async Task<Microsoft.CognitiveServices.Speech.SpeechSynthesisResult> SynthesizeAsync(Microsoft.CognitiveServices.Speech.SpeechConfig config, string content, bool isSsml)
            {
                using var synthesizer = new Microsoft.CognitiveServices.Speech.SpeechSynthesizer(config, null);
                return isSsml ? await synthesizer.SpeakSsmlAsync(content) : await synthesizer.SpeakTextAsync(content);
            }

            private static Microsoft.CognitiveServices.Speech.SpeechSynthesisOutputFormat ParseFormat(string format)
            {
                // Map a few common shorthand values; fall back to default if not matched.
                return format.ToLowerInvariant() switch
                {
                    "raw-16khz" => Microsoft.CognitiveServices.Speech.SpeechSynthesisOutputFormat.Raw16Khz16BitMonoPcm,
                    "raw-24khz" => Microsoft.CognitiveServices.Speech.SpeechSynthesisOutputFormat.Raw24Khz16BitMonoPcm,
                    "riff-24khz-16bit-mono-pcm" => Microsoft.CognitiveServices.Speech.SpeechSynthesisOutputFormat.Riff24Khz16BitMonoPcm,
                    "riff-16khz-16bit-mono-pcm" => Microsoft.CognitiveServices.Speech.SpeechSynthesisOutputFormat.Riff16Khz16BitMonoPcm,
                    _ => Microsoft.CognitiveServices.Speech.SpeechSynthesisOutputFormat.Riff24Khz16BitMonoPcm
                };
            }
}