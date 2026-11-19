using BZPlaysFoolChords;

var directory = Path.GetFullPath(args.Length > 0
    ? args[0]
    : Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "BZPlaysFoolChords", "audio"));

var steps = ChordSettings.DefaultSteps();
foreach (var instrument in InstrumentCatalog.Create().All)
{
    var folder = Path.Combine(directory, instrument.Id);
    Directory.CreateDirectory(folder);
    var count = instrument is DrumKitInstrument ? DrumKitInstrument.Patterns.Length : steps.Count;
    for (var index = 0; index < count; index++)
    {
        var clip = instrument.ClipFor(steps[Math.Min(index, steps.Count - 1)], index);
        var path = Path.Combine(folder, clip.WavFile ?? clip.Label + ".wav");
        var wav = ChordSynth.EncodeWav(clip.Synthesize());
        File.WriteAllBytes(path, wav);
        Console.WriteLine($"{path} ({wav.Length} bytes)");
    }
}
