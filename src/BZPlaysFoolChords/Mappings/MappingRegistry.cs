namespace BZPlaysFoolChords;

public sealed class MappingRegistry
{
    readonly List<ISoundMapping> _order = [];
    readonly Dictionary<string, ISoundMapping> _byKey = new(StringComparer.Ordinal);

    public IReadOnlyList<ISoundMapping> All => _order;

    public void Register(ISoundMapping mapping)
    {
        AddKey(mapping.Id, mapping);
        foreach (var alias in mapping.Aliases)
            AddKey(alias, mapping);
        _order.Add(mapping);
    }

    public bool TryGet(string requested, out ISoundMapping mapping)
    {
        if (_byKey.TryGetValue(Normalize(requested), out var found) && found is not null)
        {
            mapping = found;
            return true;
        }

        mapping = null!;
        return false;
    }

    public ISoundMapping Resolve(string requested, out string? warning)
    {
        if (TryGet(requested, out var found))
        {
            warning = null;
            return found;
        }

        warning = $"Unknown mapping '{requested}'. Using per-syllable.";
        if (!TryGet("per-syllable", out var fallback))
            throw new InvalidOperationException("The per-syllable mapping is not registered.");

        return fallback;
    }

    public static string Normalize(string value)
    {
        var chars = value.Trim().ToLowerInvariant().Where(c => c is not (' ' or '-' or '_')).ToArray();
        return new string(chars);
    }

    void AddKey(string raw, ISoundMapping mapping)
    {
        var key = Normalize(raw);
        if (key.Length == 0)
            return;

        if (_byKey.TryGetValue(key, out var existing))
        {
            if (!ReferenceEquals(existing, mapping))
                throw new InvalidOperationException($"Mapping name '{raw}' is already used by {existing.Id}.");
            return;
        }

        _byKey[key] = mapping;
    }
}
