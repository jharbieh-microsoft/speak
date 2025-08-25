# speak

Small .NET 6 console sample that demonstrates Windows text‑to‑speech (System.Speech) with both interactive and command‑line driven usage plus optional cross‑platform Azure AI Speech integration.

Core capabilities:
1. Enumerate installed Windows voices (local mode)
2. Select a voice via index, partial name, or interactively
3. Read a specified text file (default: `README.md`) aloud
4. Optional: write speech to a WAV file while also playing it live
5. Adjust speaking rate & volume
6. Use Azure AI Speech (`--azure`) for cross‑platform neural voices & SSML

## TL;DR
Build & run (Windows, .NET 6 SDK installed):

```pwsh
dotnet run -- --list                              # List local voices (System.Speech)
dotnet run                                        # Interactive selection (local)
dotnet run -- -v 1 -r 2                           # Local voice index 1, faster rate
dotnet run -- -v "Zira" -f intro.txt --save out/readme.wav --rate 1 --volume 80

# Azure mode (cross-platform). Set env vars or pass flags.
$env:AZURE_SPEECH_REGION = "eastus"
$env:AZURE_SPEECH_KEY    = "<your-key>"
dotnet run -- --azure --voice en-US-AriaNeural --file README.md --save out/aria.wav
dotnet run -- --azure --ssml aria.ssml --audio-format riff-24khz-16bit-mono-pcm --save out/ssml.wav
```

## Project Structure

```
Program.cs        # Main C# entry point using System.Speech.Synthesis
say.ps1           # Minimal PowerShell example (commented) of using System.Speech
speak.csproj      # .NET 6 project file & package references
README.md         # This document (also used as input text to speak)
```

## How It Works (Program Flow)
`Program.cs` now:

1. Parses CLI options (see below)
2. Ensures Windows platform (System.Speech is Windows-only)
3. Enumerates voices and either lists them or selects per `--voice` (index or name) / interactive fallback
4. Applies optional rate & volume tweaks
5. Reads target file (default `README.md`)
6. If `--save` specified: speaks once to the WAV file only, then switches to default device
7. Speaks the text to speakers, then a short completion message

`README.md` and `say.ps1` are marked as `Content` in the csproj so they copy to the output directory.

## Prerequisites
* Windows (the sample relies on the Windows voices exposed via `System.Speech`)
* .NET 6 SDK
* Installed Windows desktop voices (e.g., Microsoft David / Zira). Additional voices can be added via Windows Settings > Time & language > Speech.

## Build & Run
```pwsh
# Restore & build
dotnet build

# Run (from project directory)
dotnet run
```
When prompted, enter the numeric index of the voice to use. The app will then read this README followed by a sample phrase.

## Command-Line Options
Run `speak --help` (or `dotnet run -- --help`) to view usage:

```
Usage: speak [options]

Options:
  -f, --file <path>      File to read (default: README.md in current directory)
  -v, --voice <name|idx> Voice name (local: partial/idx; Azure: full neural voice name)
  -r, --rate <int>       Speaking rate (-10..10 local; Azure hint)
	  --volume <0-100>   Output volume percentage (0..100; Azure hint)
  -s, --save <wav>       Save spoken text to specified WAV file
  -l, --list             List installed local voices and exit
  -h, --help             Show this help and exit
	  --azure            Use Azure AI Speech (requires region & key)
	  --azure-region     Azure Speech region (or env AZURE_SPEECH_REGION)
	  --azure-key        Azure Speech key (or env AZURE_SPEECH_KEY)
	  --audio-format     Azure output audio format (e.g. riff-24khz-16bit-mono-pcm)
	  --ssml <file>      Provide SSML file instead of plain text (Azure mode)
```

Examples:
```pwsh
dotnet run -- --list
dotnet run -- -v 2 -f notes.txt
dotnet run -- -v "Zira" --rate 1 --volume 80 --save out/notes.wav
dotnet run -- --azure --voice en-US-JennyNeural --file notes.txt --save out/jenny.wav
dotnet run -- --azure --ssml aria.ssml --audio-format riff-24khz-16bit-mono-pcm
```

## Saving to a WAV File
Specify a target path with `--save`.

Local mode: first pass writes WAV (silent) then plays to speakers.
Azure mode: synthesized byte array is directly written.

```pwsh
# Local
dotnet run -- --file README.md --voice 0 --save out/readme.wav

# Azure
dotnet run -- --azure --voice en-US-AriaNeural --file README.md --save out/aria.wav
```

## PowerShell Example (`say.ps1`)
The script (commented out) shows equivalent usage:

```powershell
Add-Type -AssemblyName System.Speech
$speak = New-Object System.Speech.Synthesis.SpeechSynthesizer
$speak.Speak('Hello from PowerShell')
```
Uncomment the block to experiment.

## Customization Ideas (Next Enhancements)
* Batch mode: process multiple files -> multiple WAVs
* JSON / SSML input support for richer prosody control
* Cancellation / pause & resume support
* Logging verbosity levels (`--verbose`)
* Cross‑platform Azure AI Speech SDK (added)

## Known Limitations / Notes
* Local System.Speech path: Windows-only.
* Azure mode: simple SSML pass-through (no schema validation beyond file existence).
* No streaming / incremental playback yet (waits for synthesis completion).
* Rate & volume hints in Azure mode use simple property settings; prefer SSML for nuanced prosody.

## Voice Selection Implementation
Local mode: integer index or partial case-insensitive match against installed voices; fallback to first voice.
Azure mode: supply full neural voice name (e.g., `en-US-AriaNeural`). Partial name matching is not applied.

## Azure AI Speech Setup
1. Provision a Speech resource in the Azure Portal (or `az cognitiveservices account create ...`).
2. Capture Region & Key (Key1 or Key2) from the resource keys page.
3. Set environment variables (recommended):
	 ```pwsh
	 $env:AZURE_SPEECH_REGION = "eastus"
	 $env:AZURE_SPEECH_KEY = "<your-key>"
	 ```
4. Run with `--azure --voice en-US-AriaNeural` (or another neural voice). Voice list: see Azure docs "Neural voices" page.
5. (Optional) Create SSML and pass `--ssml file.ssml`.

Example SSML (`aria.ssml`):
```xml
<speak version="1.0" xml:lang="en-US">
	<voice name="en-US-AriaNeural">
		<prosody rate="+10%" pitch="+2st">Hello from Azure Speech using SSML.</prosody>
	</voice>
</speak>
```

## Dependencies
From `speak.csproj`:
* `System.Speech` (6.0.0)
* `System.Collections.NonGeneric` (4.3.0)
* `Microsoft.CognitiveServices.Speech` (1.36.0)

## Related Resources
[Text to Speech in .NET (MSDN Magazine archive)](https://docs.microsoft.com/en-us/archive/msdn-magazine/2019/june/speech-text-to-speech-synthesis-in-net)  
[Add a package with dotnet CLI](https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-add-package)  
[SpeechSynthesizer API (Framework)](https://docs.microsoft.com/en-us/dotnet/api/system.speech.synthesis.speechsynthesizer)  
[Older Windows speech samples](https://docs.microsoft.com/en-us/previous-versions/office/developer/speech-technologies/dd167624(v%3Doffice.14))

## License
Add a license (e.g., MIT) if you intend to share or reuse this sample publicly.

---
Feel free to adapt this sample for quick demos, prototyping accessibility features, or generating audio assets.
