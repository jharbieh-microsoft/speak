using System.Reflection.PortableExecutable;
using System.Speech.Synthesis;

internal class Program
{
    private static void Main(string[] args)
    {
        SpeechSynthesizer synth = new SpeechSynthesizer();  

        // Configure the audio output.   
        // synth.SetOutputToWaveFile();
        synth.SetOutputToDefaultAudioDevice();  
        
        // Voices
        foreach (var voice in synth.GetInstalledVoices()) {
            Console.WriteLine(voice.VoiceInfo.Name);

            //  Microsoft David Desktop
            //  Microsoft Zira Desktop
        }

        synth.SelectVoice("Microsoft Zira Desktop");

        // Read from file
        var root = Directory.GetCurrentDirectory();
        var path = Path.Combine(root, "README.md");

        string text = System.IO.File.ReadAllText(path);

        //  Speak from text
        synth.Speak(text);

        // Speak a string.  
        synth.Speak("Hello World! This is an example of how you may use native Windows functionality to read text. Well, I lied. It is not completely native. I had to add an assembly called: System.speech");
    }
}