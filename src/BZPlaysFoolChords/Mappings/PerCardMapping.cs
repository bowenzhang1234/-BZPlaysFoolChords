namespace BZPlaysFoolChords;

/// <summary>
/// One chord, or a sequence of chords, for a card id or a card type. Unmapped cards use the fallback.
/// </summary>
public sealed class PerCardMapping : ISoundMapping
{
    readonly Dictionary<string, MusicalRequest[]> _cards = new(StringComparer.OrdinalIgnoreCase);
    readonly Dictionary<string, MusicalRequest[]> _types = new(StringComparer.OrdinalIgnoreCase);
    readonly MusicalRequest[] _fallback;
    readonly PerSyllableMapping _syllables;
    readonly List<string> _warnings = [];
    readonly Dictionary<string, int> _counts = new(StringComparer.OrdinalIgnoreCase);

    public PerCardMapping(IReadOnlyDictionary<string, string> entries)
    {
        _syllables = new PerSyllableMapping(entries);
        foreach (var (key, value) in entries)
        {
            if (key.StartsWith("card.", StringComparison.OrdinalIgnoreCase))
                Assign(_cards, CardIds.Normalize(key["card.".Length..]), value, key);
            else if (key.StartsWith("type.", StringComparison.OrdinalIgnoreCase))
                Assign(_types, key["type.".Length..].Trim(), value, key);
        }

        entries.TryGetValue("fallback", out var fallback);
        _fallback = ChordTokens.ParseList(fallback, _warnings, "fallback").ToArray();
    }

    public string Id => "per-card";
    public string DisplayName => "Per card";
    public IEnumerable<string> Aliases => ["percard", "card"];
    public IReadOnlyList<string> StartupWarnings => _warnings;

    public MusicalRequest? Map(SoundEvent sound)
    {
        if (!string.IsNullOrEmpty(sound.CardId) && _cards.TryGetValue(sound.CardId, out var byCard))
            return Next("card:" + sound.CardId, byCard, sound);

        if (!string.IsNullOrEmpty(sound.CardType) && _types.TryGetValue(sound.CardType, out var byType))
            return Next("type:" + sound.CardType, byType, sound);

        if (_fallback.Length > 0)
            return Next("fallback", _fallback, sound);

        return _syllables.Map(sound);
    }

    void Assign(Dictionary<string, MusicalRequest[]> target, string name, string value, string key)
    {
        if (name.Length == 0)
        {
            _warnings.Add($"Ignored empty mapping '{key}'.");
            return;
        }

        var chords = ChordTokens.ParseList(value, _warnings, key);
        if (chords.Count == 0)
            _warnings.Add($"Ignored {key} because it has no chords.");
        else
            target[name] = chords.ToArray();
    }

    MusicalRequest Next(string counter, MusicalRequest[] chords, SoundEvent sound)
    {
        _counts.TryGetValue(counter, out var index);
        _counts[counter] = index + 1;
        return ChordTokens.WithSyllable(chords[index % chords.Length], MelodyEvents.Syllable(sound.EventPath) ?? "");
    }
}
