using System.Collections;
using System.Reflection.PortableExecutable;
using System.Speech.Synthesis;

internal class Program
{
    private static void Main(string[] args)
    {
        //  Get current directory
        var root = Directory.GetCurrentDirectory();

        //  File path
        var path = String.Empty;

        SpeechSynthesizer synth = new SpeechSynthesizer();

        //  Configure the audio output.   
        path = Path.Combine(root, "readout.wav");

        //  Write out to audio file
        synth.SetOutputToWaveFile(path);

        //  Write out to default audio device
        synth.SetOutputToDefaultAudioDevice();  

        //  Arraylist of voices
        var voices = new List<InstalledVoice>();

        //  List all available voices and allow user to select a voice.
        foreach (var voice in synth.GetInstalledVoices()) {
            
            //  Add voice to arraylist
            voices.Add(voice);

            //  Display the voice's name.
            //  Console.WriteLine("Voice Name: " + voice.VoiceInfo.Name);
        };

        for (int i = 0; i < voices.Count; i++)
        {
            //  Microsoft David Desktop
            //  Microsoft Zira Desktop 

            Console.WriteLine("[{0}]:{1}",i,voices[i].VoiceInfo.Name);
        }

        //  print out an empty line
        Console.WriteLine();

        //  Prompt the user to select a voice
        Console.WriteLine("Select a voice: ");

        var voiceSelection = Console.ReadLine();

        //  Switch to selected voice
        switch (voiceSelection)
        {
            case "0":
                synth.SelectVoice(voices[0].VoiceInfo.Name);
                break;
            case "1":
                synth.SelectVoice(voices[1].VoiceInfo.Name);
                break;
            default:
                synth.SelectVoice(voices[0].VoiceInfo.Name);
                break;
        }

        //  Select a voice
        //  synth.SelectVoice("Microsoft Zira Desktop");

        // Read from file
        path = Path.Combine(root, "README.md");
        string text = System.IO.File.ReadAllText(path);

        //  Speak from text
        synth.Speak(text);

        // Speak a string.  
        synth.Speak("Hello World! This is an example of how you may use native Windows functionality to read text. Well, I lied. It is not completely native. I had to add an assembly called: System.speech");
    }
}