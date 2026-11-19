namespace BZPlaysFoolChords;

public enum PlayAction
{
    Ignore,
    PlayChord,
    SuppressOnly,
}

public readonly record struct PlayResult(PlayAction Action, int ChordIndex);

public sealed class ProgressionState
{
    public int NextIndex { get; private set; }
    public long LastChordMs { get; private set; } = long.MinValue;
    public string? LastCardId { get; private set; }

    public PlayResult Consider(string? sfx, string? cardId, ChordSettings settings, long nowMs)
    {
        if (!MelodyEvents.IsMelodyVoice(sfx) || settings.Steps.Count == 0)
            return new PlayResult(PlayAction.Ignore, -1);

        if (settings.CardIds.Count > 0 && (string.IsNullOrEmpty(cardId) || !settings.CardIds.Contains(cardId)))
            return new PlayResult(PlayAction.Ignore, -1);

        var withinWindow = LastChordMs != long.MinValue && nowMs >= LastChordMs && nowMs - LastChordMs < settings.DebounceMs;
        var sameGesture = withinWindow && CardsMatch(cardId, LastCardId);
        if (sameGesture)
            return new PlayResult(settings.ReplaceVanilla ? PlayAction.SuppressOnly : PlayAction.Ignore, -1);

        var index = NextIndex % settings.Steps.Count;
        NextIndex = index + 1;
        LastChordMs = nowMs;
        LastCardId = cardId;
        return new PlayResult(PlayAction.PlayChord, index);
    }

    static bool CardsMatch(string? current, string? previous)
    {
        if (string.IsNullOrEmpty(current) || string.IsNullOrEmpty(previous))
            return true;

        return string.Equals(current, previous, StringComparison.OrdinalIgnoreCase);
    }
}
