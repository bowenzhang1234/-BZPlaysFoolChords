using BZPlaysFoolChords;

var directory = Path.GetFullPath(args.Length > 0
    ? args[0]
    : Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "BZPlaysFoolChords", "audio", "piano"));

Directory.CreateDirectory(directory);
foreach (var (name, notes) in ChordLibrary.Defaults)
{
    var path = Path.Combine(directory, name + ".wav");
    var wav = ChordSynth.EncodeWav(PianoInstrument.Render(notes.Split(',')));
    File.WriteAllBytes(path, wav);
    Console.WriteLine($"{path} ({wav.Length} bytes)");
}
