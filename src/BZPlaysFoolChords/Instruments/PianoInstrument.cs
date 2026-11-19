namespace BZPlaysFoolChords;

public sealed class PianoInstrument : IInstrument
{
    public string Id => "piano";
    public string DisplayName => "Piano";
    public IEnumerable<string> Aliases => [];

    public InstrumentClip? ClipFor(InstrumentRequest request)
    {
        var notes = request.Music.Notes.Length > 0
            ? request.Music.Notes
            : ChordLibrary.NotesFor(request.Music.Label) ?? ["C4", "E4", "G4"];
        return PitchedClip.Create(this, request.Music, notes, () => Render(notes));
    }

    public static short[] Render(IReadOnlyList<string> notes)
    {
        var mix = new double[SynthMath.Length()];
        foreach (var note in notes)
            AddHammer(mix, NoteMath.Frequency(note));

        return SynthMath.ToPcm(mix);
    }

    static void AddHammer(double[] mix, double frequency)
    {
        const double inharmonicity = 0.00045;
        for (var partial = 1; partial <= 7; partial++)
        {
            var stretched = frequency * partial * Math.Sqrt(1 + inharmonicity * partial * partial);
            var amplitude = 1.0 / Math.Pow(partial, 1.25);
            var decay = 0.95 / partial;
            SynthMath.AddSine(mix, stretched, amplitude, decay, attackSamples: partial == 1 ? 180 : 40);
        }

        SynthMath.AddNoise(mix, 0, 500, 0.18, SynthMath.Seed(frequency, 3), decaySamples: 70);
    }
}
