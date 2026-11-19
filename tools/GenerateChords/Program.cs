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
        var clip = instrument.ClipFor(new InstrumentRequest(steps[Math.Min(index, steps.Count - 1)], index, null));
        if (clip?.Synthesize is not Func<short[]> synthesize || string.IsNullOrEmpty(clip.WavFile))
            continue;

        var path = Path.Combine(folder, clip.WavFile);
        var wav = ChordSynth.EncodeWav(synthesize());
        File.WriteAllBytes(path, wav);
        Console.WriteLine($"{path} ({wav.Length} bytes)");
    }
}
