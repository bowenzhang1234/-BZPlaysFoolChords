namespace BZPlaysFoolChords;

/// <summary>
/// What the card-play hook knows about one play. Instruments use the card id for per-card sounds.
/// The hook does not interpret it.
/// </summary>
public readonly record struct InstrumentRequest(MusicalRequest Music, string? CardId);

/// <summary>
/// One playable voice. The card-play hook asks the active instrument for a clip and never branches on which voice that is.
/// Return null to leave the game's own melody sound in place.
/// </summary>
public interface IInstrument
{
    string Id { get; }
    string DisplayName { get; }
    IEnumerable<string> Aliases { get; }
    IReadOnlyList<string> StartupWarnings => [];
    InstrumentClip? ClipFor(InstrumentRequest request);
}

public sealed class InstrumentClip
{
    public required string InstrumentId { get; init; }
    public required string Label { get; init; }
    public string? WavFile { get; init; }
    public string? AudioPath { get; init; }
    public required string CacheKey { get; init; }
    public Func<short[]>? Synthesize { get; init; }
}

public static class PitchedClip
{
    public static InstrumentClip Create(IInstrument instrument, MusicalRequest music, IReadOnlyList<string> notes, Func<short[]> synthesize) =>
        new()
        {
            InstrumentId = instrument.Id,
            Label = music.Label,
            WavFile = string.IsNullOrEmpty(music.WavFile) ? music.Label + ".wav" : music.WavFile,
            CacheKey = instrument.Id + "|" + music.Label + "|" + string.Join(',', notes),
            Synthesize = synthesize,
        };
}
