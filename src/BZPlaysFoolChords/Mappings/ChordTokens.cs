namespace BZPlaysFoolChords;

public static class ChordTokens
{
    public static List<MusicalRequest> ParseList(string? value, ICollection<string> warnings, string settingName)
    {
        var chords = new List<MusicalRequest>();
        if (string.IsNullOrWhiteSpace(value))
            return chords;

        foreach (var token in value.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (TryParse(token, out var chord))
                chords.Add(chord);
            else
                warnings.Add($"Skipped {settingName} chord '{token}'.");
        }

        return chords;
    }

    public static bool TryParse(string token, out MusicalRequest chord)
    {
        chord = default;
        token = token.Trim();
        if (token.Length == 0)
            return false;

        try
        {
            if (token.EndsWith(".wav", StringComparison.OrdinalIgnoreCase) || token.EndsWith(".ogg", StringComparison.OrdinalIgnoreCase))
            {
                var file = Path.GetFileName(token);
                if (file != token || file.Contains("..", StringComparison.Ordinal))
                    return false;

                var label = Path.GetFileNameWithoutExtension(file);
                chord = new MusicalRequest(label, "", ChordLibrary.NotesFor(label) ?? [], file, false);
                return true;
            }

            var labelText = token;
            var notesText = token;
            var colon = token.IndexOf(':');
            if (colon > 0)
            {
                labelText = token[..colon].Trim();
                notesText = token[(colon + 1)..].Trim();
            }

            var library = ChordLibrary.NotesFor(labelText);
            if (colon < 0 && library != null)
            {
                chord = new MusicalRequest(libraryLabel(labelText), "", library, libraryLabel(labelText) + ".wav", false);
                return true;
            }

            var notes = notesText.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (notes.Length == 0)
                return false;

            foreach (var note in notes)
                NoteMath.Frequency(note);

            chord = new MusicalRequest(labelText, "", notes, null, true);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    public static MusicalRequest WithSyllable(MusicalRequest chord, string syllable) =>
        chord with { Syllable = syllable };

    static string libraryLabel(string name)
    {
        foreach (var (chordName, _) in ChordLibrary.Defaults)
        {
            if (string.Equals(chordName, name, StringComparison.OrdinalIgnoreCase))
                return chordName;
        }

        return name;
    }
}
