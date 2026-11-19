namespace BZPlaysFoolChords;

/// <summary>
/// Plays wav or ogg files from a folder. Adding a recording needs no code change:
/// name the file after the syllable (wah.wav), the chord (C.wav or Am.ogg), or the card id.
/// </summary>
public sealed class SampleFolderInstrument : IInstrument
{
    readonly string _folder;

    public SampleFolderInstrument(string folder) => _folder = folder;

    public string Id => "samples";
    public string DisplayName => "Sample folder";
    public IEnumerable<string> Aliases => ["sample", "voice", "folder"];

    public IReadOnlyList<string> StartupWarnings =>
        Directory.Exists(_folder)
            ? []
            : [$"Sample folder '{_folder}' does not exist yet. Create it and drop in wav or ogg files."];

    public InstrumentClip? ClipFor(InstrumentRequest request)
    {
        foreach (var name in FileNames(request))
        {
            var path = Path.Combine(_folder, name);
            if (!File.Exists(path))
                continue;

            return new InstrumentClip
            {
                InstrumentId = Id,
                Label = request.Music.Label,
                WavFile = name,
                AudioPath = path,
                CacheKey = path,
                Synthesize = null,
            };
        }

        return null;
    }

    public static IEnumerable<string> FileNames(InstrumentRequest request)
    {
        var names = new List<string>();
        Add(names, request.Music.Syllable);
        Add(names, request.Music.Label);
        Add(names, request.CardId);
        if (!string.IsNullOrEmpty(request.Music.WavFile))
            Add(names, Path.GetFileNameWithoutExtension(request.Music.WavFile));

        return names;
    }

    static void Add(List<string> names, string? stem)
    {
        if (string.IsNullOrWhiteSpace(stem))
            return;

        var file = stem.Trim();
        if (file != Path.GetFileName(file) || file.Contains("..", StringComparison.Ordinal))
            return;

        if (file.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            return;

        foreach (var extension in new[] { ".wav", ".ogg" })
        {
            var name = file.EndsWith(extension, StringComparison.OrdinalIgnoreCase) ? file : file + extension;
            if (!names.Contains(name, StringComparer.OrdinalIgnoreCase))
                names.Add(name);
        }
    }
}
