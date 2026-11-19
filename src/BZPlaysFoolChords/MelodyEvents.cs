namespace BZPlaysFoolChords;

/// <summary>
/// Voice stingers played by the Fool melody VFX. The character's audio id in the banks is "fugitive".
/// </summary>
public static class MelodyEvents
{
    public const string Wah = "event:/sfx/characters/fugitive/fugitive_wa";
    public const string Cho = "event:/sfx/characters/fugitive/fugitive_cho";
    public const string Ki = "event:/sfx/characters/fugitive/fugitive_ki";

    public static bool IsMelodyVoice(string? sfx) =>
        sfx is Wah or Cho or Ki;
}
