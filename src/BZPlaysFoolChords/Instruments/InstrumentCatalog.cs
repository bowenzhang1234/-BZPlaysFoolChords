namespace BZPlaysFoolChords;

/// <summary>
/// The only list of built-in voices. Add a new instrument by registering it here.
/// </summary>
public static class InstrumentCatalog
{
    public static InstrumentRegistry Create()
    {
        var registry = new InstrumentRegistry();
        registry.Register(new PianoInstrument());
        registry.Register(new AcousticGuitarInstrument());
        registry.Register(new ElectricGuitarInstrument());
        registry.Register(new JazzRhodesInstrument());
        registry.Register(new DrumKitInstrument());
        return registry;
    }
}
