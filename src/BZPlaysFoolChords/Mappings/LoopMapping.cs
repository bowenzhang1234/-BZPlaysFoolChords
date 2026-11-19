namespace BZPlaysFoolChords;

/// <summary>
/// Walks a chord list in order. The syllable and the card do not choose the chord.
/// </summary>
public sealed class LoopMapping : ISoundMapping
{
    readonly List<MusicalRequest> _chords;
    readonly List<string> _warnings = [];
    int _index;

    public LoopMapping(IReadOnlyDictionary<string, string> entries)
    {
        entries.TryGetValue("progression", out var progression);
        _chords = ChordTokens.ParseList(progression, _warnings, "progression");
        if (_chords.Count == 0)
        {
            _warnings.Add("Loop progression was empty. Using C, G, Am, F.");
            _chords = ChordTokens.ParseList("C | G | Am | F", _warnings, "progression");
        }
    }

    public string Id => "loop";
    public string DisplayName => "Loop";
    public IEnumerable<string> Aliases => ["cycle", "order"];
    public IReadOnlyList<string> StartupWarnings => _warnings;

    public MusicalRequest? Map(SoundEvent sound)
    {
        var chord = _chords[_index % _chords.Count];
        _index++;
        return ChordTokens.WithSyllable(chord, MelodyEvents.Syllable(sound.EventPath) ?? "");
    }
}
