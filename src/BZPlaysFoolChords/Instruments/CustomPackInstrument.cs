namespace BZPlaysFoolChords;

/// <summary>
/// A user sound pack. Files are 16-bit wavs in audio/custom, named by pack.cfg in that folder.
/// This is another IInstrument: the card-play hook does not special-case it.
/// </summary>
public sealed class CustomPackInstrument : IInstrument
{
    readonly CustomPack _pack;
    readonly Dictionary<string, int> _cardPlays = new(StringComparer.OrdinalIgnoreCase);

    public CustomPackInstrument(CustomPack pack) => _pack = pack;

    public string Id => "custom";
    public string DisplayName => "Custom pack";
    public IEnumerable<string> Aliases => ["pack", "custompack"];
    public IReadOnlyList<string> StartupWarnings =>
        _pack.Files.Count == 0 && _pack.Cards.Count == 0
            ? [.._pack.Warnings, "Custom pack has no clips yet. Add wav names to audio/custom/pack.cfg."]
            : _pack.Warnings;

    public InstrumentClip? ClipFor(InstrumentRequest request)
    {
        if (!string.IsNullOrEmpty(request.CardId) && _pack.Cards.TryGetValue(request.CardId, out var cardFiles) && cardFiles.Length > 0)
        {
            _cardPlays.TryGetValue(request.CardId, out var played);
            _cardPlays[request.CardId] = played + 1;
            return FileClip(cardFiles[played % cardFiles.Length], request.CardId);
        }

        if (_pack.Files.Count == 0)
            return null;

        var file = _pack.Files[Math.Abs(request.StepIndex) % _pack.Files.Count];
        return FileClip(file, request.Step.Label);
    }

    InstrumentClip FileClip(string file, string label) => new()
    {
        InstrumentId = Id,
        Label = Path.GetFileNameWithoutExtension(file),
        WavFile = file,
        CacheKey = Id + "|" + label + "|" + file,
        Synthesize = null,
    };
}

public sealed class CustomPack
{
    public IReadOnlyList<string> Files { get; private init; } = [];
    public IReadOnlyDictionary<string, string[]> Cards { get; private init; } = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
    public IReadOnlyList<string> Warnings { get; private init; } = [];

    public static CustomPack Empty { get; } = new();

    public static CustomPack Load(string path) =>
        File.Exists(path) ? Parse(File.ReadAllText(path)) : Empty;

    public static CustomPack Parse(string text)
    {
        var files = new List<string>();
        var cards = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        var warnings = new List<string>();

        foreach (var raw in text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line[0] is '#' or ';')
                continue;

            var split = line.IndexOf('=');
            if (split <= 0)
            {
                warnings.Add($"Ignored custom pack line '{line}'.");
                continue;
            }

            var key = line[..split].Trim();
            var value = line[(split + 1)..].Trim();
            if (key.Equals("files", StringComparison.OrdinalIgnoreCase))
            {
                files.AddRange(Wavs(value, warnings));
                continue;
            }

            if (key.StartsWith("card.", StringComparison.OrdinalIgnoreCase))
            {
                var cardId = CardIds.Normalize(key["card.".Length..]);
                var wavs = Wavs(value, warnings);
                if (cardId.Length == 0 || wavs.Count == 0)
                    warnings.Add($"Ignored card mapping '{key}'.");
                else
                    cards[cardId] = wavs.ToArray();
                continue;
            }

            warnings.Add($"Unknown custom pack setting '{key}'.");
        }

        return new CustomPack { Files = files, Cards = cards, Warnings = warnings };
    }

    static List<string> Wavs(string value, List<string> warnings)
    {
        var wavs = new List<string>();
        foreach (var part in value.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (IsSafeWav(part))
                wavs.Add(Path.GetFileName(part));
            else
                warnings.Add($"Ignored '{part}'. Custom clips must be wav file names in the pack folder.");
        }

        return wavs;
    }

    public static bool IsSafeWav(string name)
    {
        var file = name.Trim();
        return file.Length > 0
            && file.EndsWith(".wav", StringComparison.OrdinalIgnoreCase)
            && file == Path.GetFileName(file)
            && !file.Contains("..", StringComparison.Ordinal)
            && file.IndexOfAny(Path.GetInvalidFileNameChars()) < 0;
    }
}
