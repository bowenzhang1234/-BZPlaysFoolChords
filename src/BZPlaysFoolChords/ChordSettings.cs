using System.Globalization;

namespace BZPlaysFoolChords;

public sealed record ChordStep(string Label, string[] Notes, string? WavFile);

public enum ChordMode
{
    Replace,
    Alongside,
}

public sealed class ChordSettings
{
    public ChordMode Mode { get; init; } = ChordMode.Replace;
    public int DebounceMs { get; init; } = 1100;
    public float Volume { get; init; } = 1f;
    public IReadOnlySet<string> CardIds { get; init; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    public IReadOnlyList<ChordStep> Steps { get; init; } = [];
    public IReadOnlyList<string> Warnings { get; init; } = [];

    public bool ReplaceVanilla => Mode == ChordMode.Replace;

    public static ChordSettings Parse(string text)
    {
        var mode = ChordMode.Replace;
        var debounce = 1100;
        var volume = 1f;
        var cardIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        string? progression = null;
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
            switch (key.ToLowerInvariant())
            {
                case "mode":
                    if (value.Equals("replace", StringComparison.OrdinalIgnoreCase))
                        mode = ChordMode.Replace;
                    else if (value.Equals("alongside", StringComparison.OrdinalIgnoreCase))
                        mode = ChordMode.Alongside;
                    else
                        warnings.Add($"Unknown mode '{value}'. Using replace.");
                    break;
                case "debouncems":
                    if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) && parsed >= 0)
                        debounce = parsed;
                    else
                        warnings.Add($"debounceMs '{value}' is not a whole number. Using {debounce}.");
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
                case "progression":
                    progression = value;
                    break;
                default:
                    warnings.Add($"Unknown setting '{key}'.");
                    break;
            }
        }

        var steps = ParseProgression(progression, warnings);
        if (steps.Count == 0)
        {
            warnings.Add("Progression was empty. Using C, G, Am, F.");
            steps = DefaultSteps();
        }

        return new ChordSettings
        {
            Mode = mode,
            DebounceMs = debounce,
            Volume = volume,
            CardIds = cardIds,
            Steps = steps,
            Warnings = warnings,
        };
    }

    public static List<ChordStep> DefaultSteps() =>
        ChordLibrary.Defaults.Select(chord => new ChordStep(chord.Name, chord.Notes.Split(','), chord.Name + ".wav")).ToList();

    static List<ChordStep> ParseProgression(string? progression, List<string> warnings)
    {
        if (string.IsNullOrWhiteSpace(progression))
            return DefaultSteps();

        var steps = new List<ChordStep>();
        foreach (var token in progression.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            try
            {
                steps.Add(ParseStep(token));
            }
            catch (FormatException error)
            {
                warnings.Add($"Skipped chord '{token}': {error.Message}");
            }
        }

        return steps;
    }

    static ChordStep ParseStep(string token)
    {
        if (token.EndsWith(".wav", StringComparison.OrdinalIgnoreCase))
        {
            var file = Path.GetFileName(token);
            var label = Path.GetFileNameWithoutExtension(file);
            return new ChordStep(label, ChordLibrary.NotesFor(label) ?? [], file);
        }

        var labelText = token;
        var notesText = token;
        var colon = token.IndexOf(':');
        if (colon > 0)
        {
            labelText = token[..colon].Trim();
            notesText = token[(colon + 1)..].Trim();
        }

        var notes = notesText.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (notes.Length == 0)
            throw new FormatException("no notes were listed.");

        foreach (var note in notes)
            NoteMath.Frequency(note);

        return new ChordStep(labelText, notes, null);
    }
}
