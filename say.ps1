# How to read text in PowerShell on a Windows machine
Add-Type -AssemblyName System.speech
$speak = New-Object System.Speech.Synthesis.SpeechSynthesizer
$speak.Speak('Hello World! This is an example of how you may use native Windows functionality to read text. Well, I lied. It is not completely native. I had to add an assembly called: System.speech')