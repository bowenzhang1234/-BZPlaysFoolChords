namespace BZPlaysFoolChords;

/// <summary>
/// The only list of chord-selection strategies. Add a new mapping by registering it here.
/// </summary>
public static class MappingCatalog
{
    public static MappingRegistry Create(IReadOnlyDictionary<string, string> entries)
    {
        var registry = new MappingRegistry();
        registry.Register(new PerSyllableMapping(entries));
        registry.Register(new LoopMapping(entries));
        registry.Register(new PerCardMapping(entries));
        registry.Register(new RandomMapping(entries));
        return registry;
    }
}
