namespace BZPlaysFoolChords;

/// <summary>
/// I–V–vi–IV in C. The fifth sits under the tonic so successive plays move instead of stacking the same shape.
/// </summary>
public static class ChordLibrary
{
    public static readonly (string Name, string Notes)[] Defaults =
    [
        ("C", "C4,E4,G4"),
        ("G", "G3,B3,D4"),
        ("Am", "A3,C4,E4"),
        ("F", "F3,A3,C4"),
    ];

    public static string[]? NotesFor(string name)
    {
        foreach (var (chordName, notes) in Defaults)
        {
            if (string.Equals(chordName, name, StringComparison.OrdinalIgnoreCase))
                return notes.Split(',');
        }

        return null;
    }
}
