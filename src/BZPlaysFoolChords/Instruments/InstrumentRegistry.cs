namespace BZPlaysFoolChords;

public sealed class InstrumentRegistry
{
    readonly List<IInstrument> _order = [];
    readonly Dictionary<string, IInstrument> _byKey = new(StringComparer.Ordinal);

    public IReadOnlyList<IInstrument> All => _order;

    public void Register(IInstrument instrument)
    {
        AddKey(instrument.Id, instrument);
        foreach (var alias in instrument.Aliases)
            AddKey(alias, instrument);
        _order.Add(instrument);
    }

    public bool TryGet(string requested, out IInstrument instrument)
    {
        if (_byKey.TryGetValue(Normalize(requested), out var found) && found is not null)
        {
            instrument = found;
            return true;
        }

        instrument = null!;
        return false;
    }

    public IInstrument Resolve(string requested, out string? warning)
    {
        if (TryGet(requested, out var found))
        {
            warning = null;
            return found;
        }

        warning = $"Unknown instrument '{requested}'. Using piano.";
        if (!TryGet("piano", out var piano))
            throw new InvalidOperationException("The piano instrument is not registered.");

        return piano;
    }

    public static string Normalize(string value)
    {
        var chars = value.Trim().ToLowerInvariant().Where(c => c is not (' ' or '-' or '_')).ToArray();
        return new string(chars);
    }

    void AddKey(string raw, IInstrument instrument)
    {
        var key = Normalize(raw);
        if (key.Length == 0)
            return;

        if (_byKey.TryGetValue(key, out var existing))
        {
            if (!ReferenceEquals(existing, instrument))
                throw new InvalidOperationException($"Instrument name '{raw}' is already used by {existing.Id}.");
            return;
        }

        _byKey[key] = instrument;
    }
}
