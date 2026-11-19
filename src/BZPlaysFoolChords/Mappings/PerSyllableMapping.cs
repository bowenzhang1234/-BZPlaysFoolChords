namespace BZPlaysFoolChords;

/// <summary>
/// Wah, Cho, and Ki each play one chord. A compound card plays every syllable's chord in order.
/// </summary>
public sealed class PerSyllableMapping : ISoundMapping
{
    readonly Dictionary<string, MusicalRequest> _chords;
    readonly List<string> _warnings = [];

    public PerSyllableMapping(IReadOnlyDictionary<string, string> entries)
    {
        _chords = new Dictionary<string, MusicalRequest>(StringComparer.OrdinalIgnoreCase)
        {
            ["wah"] = Default("C"),
            ["cho"] = Default("G"),
            ["ki"] = Default("Am"),
        };

        foreach (var (key, value) in entries)
        {
            if (!key.StartsWith("syllable.", StringComparison.OrdinalIgnoreCase))
                continue;

            var syllable = key["syllable.".Length..].Trim().ToLowerInvariant();
            if (syllable is not ("wah" or "cho" or "ki"))
            {
                _warnings.Add($"Unknown syllable '{syllable}'. Use wah, cho, or ki.");
                continue;
            }

            if (!ChordTokens.TryParse(value, out var chord))
            {
                _warnings.Add($"Could not read syllable.{syllable} chord '{value}'.");
                continue;
            }

            _chords[syllable] = chord;
        }
    }

    public string Id => "per-syllable";
    public string DisplayName => "Per syllable";
    public IEnumerable<string> Aliases => ["syllable", "persyllable"];
    public IReadOnlyList<string> StartupWarnings => _warnings;

    public MusicalRequest? Map(SoundEvent sound)
    {
        var syllable = MelodyEvents.Syllable(sound.EventPath);
        if (syllable == null || !_chords.TryGetValue(syllable, out var chord))
            return null;

        return ChordTokens.WithSyllable(chord, syllable);
    }

    static MusicalRequest Default(string name)
    {
        ChordTokens.TryParse(name, out var chord);
        return chord;
    }
}
