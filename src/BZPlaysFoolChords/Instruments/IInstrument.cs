namespace BZPlaysFoolChords;

/// <summary>
/// One playable voice. The card-play hook asks the active instrument for a clip and never branches on which voice that is.
/// </summary>
public interface IInstrument
{
    string Id { get; }
    string DisplayName { get; }
    IEnumerable<string> Aliases { get; }
    InstrumentClip ClipFor(ChordStep step, int stepIndex);
}

public sealed class InstrumentClip
{
    public required string InstrumentId { get; init; }
    public required string Label { get; init; }
    public string? WavFile { get; init; }
    public required string CacheKey { get; init; }
    public required Func<short[]> Synthesize { get; init; }
}

public static class PitchedClip
{
    public static string[] Notes(ChordStep step) =>
        step.Notes.Length > 0 ? step.Notes : ["C4", "E4", "G4"];

    public static InstrumentClip Create(IInstrument instrument, ChordStep step, IReadOnlyList<string> notes, Func<short[]> synthesize) =>
        new()
        {
            InstrumentId = instrument.Id,
            Label = step.Label,
            WavFile = string.IsNullOrEmpty(step.WavFile) ? step.Label + ".wav" : step.WavFile,
            CacheKey = instrument.Id + "|" + step.Label + "|" + string.Join(',', notes),
            Synthesize = synthesize,
        };
}
