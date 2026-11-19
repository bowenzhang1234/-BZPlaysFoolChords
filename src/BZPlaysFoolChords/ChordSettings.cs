using System.Globalization;

namespace BZPlaysFoolChords;

public enum ChordMode
{
    Replace,
    Alongside,
}

public sealed class ChordSettings
{
    public ChordMode Mode { get; init; } = ChordMode.Replace;
    public string Instrument { get; init; } = "piano";
    public string Mapping { get; init; } = "per-syllable";
    public string Samples { get; init; } = Path.Combine("audio", "samples");
    public float Volume { get; init; } = 1f;
    public IReadOnlySet<string> CardIds { get; init; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    public IReadOnlyDictionary<string, string> Entries { get; init; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    public IReadOnlyList<string> Warnings { get; init; } = [];

    public bool ReplaceVanilla => Mode == ChordMode.Replace;

    public static string ResolveSamplesDirectory(string modDirectory, string configured) =>
        Path.IsPathRooted(configured) ? configured : Path.GetFullPath(Path.Combine(modDirectory, configured));

    public static ChordSettings Parse(string text)
    {
        var mode = ChordMode.Replace;
        var instrument = "piano";
        var mapping = "per-syllable";
        var samples = Path.Combine("audio", "samples");
        var volume = 1f;
        var cardIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var entries = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var warnings = new List<string>();

        foreach (var raw in text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line[0] is '#' or ';')
                continue;

            var split = line.IndexOf('=');
            if (split <= 0)
            {
                warnings.Add($"Ignored '{line}' because it has no '='.");
                continue;
            }

            var key = line[..split].Trim();
            var value = line[(split + 1)..].Trim();
            entries[key] = value;
            switch (key.ToLowerInvariant())
            {
                case "mode":
                    if (value.Equals("replace", StringComparison.OrdinalIgnoreCase))
                        mode = ChordMode.Replace;
                    else if (value.Equals("alongside", StringComparison.OrdinalIgnoreCase))
                        mode = ChordMode.Alongside;
                    else
                        warnings.Add($"Unknown mode '{value}'. Using replace. Chord choice uses the mapping setting.");
                    break;
                case "instrument":
                    instrument = string.IsNullOrWhiteSpace(value) ? "piano" : value.Trim();
                    break;
                case "mapping":
                    mapping = string.IsNullOrWhiteSpace(value) ? "per-syllable" : value.Trim();
                    break;
                case "samples":
                    if (!string.IsNullOrWhiteSpace(value))
                        samples = value.Trim();
                    break;
                case "debouncems":
                    warnings.Add("debounceMs is ignored. Each Wah, Cho, and Ki syllable plays its own sound.");
                    break;
                case "volume":
                    if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var gain) && gain >= 0)
                        volume = gain;
                    else
                        warnings.Add($"volume '{value}' is not a number. Using {volume.ToString(CultureInfo.InvariantCulture)}.");
                    break;
                case "cardids":
                    foreach (var part in value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                    {
                        var id = global::BZPlaysFoolChords.CardIds.Normalize(part);
                        if (id.Length > 0)
                            cardIds.Add(id);
                    }
                    break;
            }
        }

        return new ChordSettings
        {
            Mode = mode,
            Instrument = instrument,
            Mapping = mapping,
            Samples = samples,
            Volume = volume,
            CardIds = cardIds,
            Entries = entries,
            Warnings = warnings,
        };
    }
}
