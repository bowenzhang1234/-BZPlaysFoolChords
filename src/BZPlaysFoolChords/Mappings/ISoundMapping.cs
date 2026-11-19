namespace BZPlaysFoolChords;

/// <summary>
/// One melody sound the hook observed. Mappings turn this into a chord. The hook does not interpret it.
/// </summary>
public readonly record struct SoundEvent(string EventPath, string? CardId, string? CardType, int PlayIndex);

/// <summary>
/// What an instrument should play. Instruments do not know which mapping produced it.
/// </summary>
public readonly record struct MusicalRequest(string Label, string Syllable, string[] Notes, string? WavFile, bool CustomNotes);

/// <summary>
/// Chooses the chord for one melody sound. The card-play hook and the instruments never branch on which mapping this is.
/// Return null to leave the game's own sound in place.
/// </summary>
public interface ISoundMapping
{
    string Id { get; }
    string DisplayName { get; }
    IEnumerable<string> Aliases { get; }
    IReadOnlyList<string> StartupWarnings => [];
    MusicalRequest? Map(SoundEvent sound);
}
