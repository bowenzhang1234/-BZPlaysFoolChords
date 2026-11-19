namespace BZPlaysFoolChords;

/// <summary>
/// The only list of sound sources. This PR ships piano plus a folder of user recordings.
/// Add another synthesized voice by registering it here.
/// </summary>
public static class InstrumentCatalog
{
    public static InstrumentRegistry Create(string samplesDirectory)
    {
        var registry = new InstrumentRegistry();
        registry.Register(new PianoInstrument());
        registry.Register(new SampleFolderInstrument(samplesDirectory));
        return registry;
    }
}
