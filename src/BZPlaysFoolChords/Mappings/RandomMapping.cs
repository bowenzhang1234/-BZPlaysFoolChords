namespace BZPlaysFoolChords;

/// <summary>
/// Picks a chord at random from a pool. The syllable and the card do not choose it.
/// </summary>
public sealed class RandomMapping : ISoundMapping
{
    readonly List<MusicalRequest> _pool;
    readonly List<string> _warnings = [];
    readonly Random _random;

    public RandomMapping(IReadOnlyDictionary<string, string> entries, Random? random = null)
    {
        _random = random ?? Random.Shared;
        if (!entries.TryGetValue("pool", out var pool))
            entries.TryGetValue("progression", out pool);

        _pool = ChordTokens.ParseList(pool, _warnings, "pool");
        if (_pool.Count == 0)
        {
            _warnings.Add("Random pool was empty. Using C, G, Am, F.");
            _pool = ChordTokens.ParseList("C | G | Am | F", _warnings, "pool");
        }
    }

    public string Id => "random";
    public string DisplayName => "Random";
    public IEnumerable<string> Aliases => ["shuffle"];
    public IReadOnlyList<string> StartupWarnings => _warnings;

    public MusicalRequest? Map(SoundEvent sound)
    {
        var chord = _pool[_random.Next(_pool.Count)];
        return ChordTokens.WithSyllable(chord, MelodyEvents.Syllable(sound.EventPath) ?? "");
    }
}
