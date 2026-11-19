using MegaCrit.Sts2.Core.Modding;

namespace BZPlaysFoolChords;

[ModInitializer(nameof(Initialize))]
public static class ModEntry
{
    public static void Initialize()
    {
        try
        {
            ChordRuntime.Start();
        }
        catch (Exception error)
        {
            GameLog.Warn("Failed to start, so melody sounds stay vanilla: " + error);
        }
    }
}
