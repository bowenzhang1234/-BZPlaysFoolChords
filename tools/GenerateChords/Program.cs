using BZPlaysFoolChords;

var directory = Path.GetFullPath(args.Length > 0
    ? args[0]
    : Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "BZPlaysFoolChords", "audio"));

Directory.CreateDirectory(directory);
foreach (var (name, notes) in ChordLibrary.Defaults)
{
    var wav = ChordSynth.RenderWav(notes.Split(','));
    var path = Path.Combine(directory, name + ".wav");
    File.WriteAllBytes(path, wav);
    Console.WriteLine($"{path} ({wav.Length} bytes)");
}
