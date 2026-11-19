namespace BZPlaysFoolChords;

/// <summary>
/// The only list of built-in voices. Add a new instrument by registering it here.
/// </summary>
public static class InstrumentCatalog
{
    public static InstrumentRegistry Create()
    {
        var directory = Path.GetDirectoryName(typeof(InstrumentCatalog).Assembly.Location) ?? ".";
        var packPath = Path.Combine(directory, "audio", "custom", "pack.cfg");
        return Create(CustomPack.Load(packPath));
    }

    public static InstrumentRegistry Create(CustomPack customPack)
    {
        var registry = new InstrumentRegistry();
        registry.Register(new PianoInstrument());
        registry.Register(new AcousticGuitarInstrument());
        registry.Register(new ElectricGuitarInstrument());
        registry.Register(new JazzRhodesInstrument());
        registry.Register(new DrumKitInstrument());
        registry.Register(new CustomPackInstrument(customPack));
        return registry;
    }
}
