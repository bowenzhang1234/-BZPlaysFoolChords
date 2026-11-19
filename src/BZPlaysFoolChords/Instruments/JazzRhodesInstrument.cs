namespace BZPlaysFoolChords;

public sealed class JazzRhodesInstrument : IInstrument
{
    public string Id => "jazz";
    public string DisplayName => "Jazz piano";
    public IEnumerable<string> Aliases => ["rhodes", "jazzpiano"];

    static readonly Dictionary<string, string[]> Voicings = new(StringComparer.OrdinalIgnoreCase)
    {
        ["C"] = ["E4", "G4", "B4", "D5"],
        ["G"] = ["B3", "D4", "F#4", "A4"],
        ["Am"] = ["A3", "C4", "E4", "G4", "B4"],
        ["F"] = ["A3", "C4", "E4", "G4"],
    };

    public InstrumentClip? ClipFor(InstrumentRequest request)
    {
        var step = request.Step;
        var notes = step.CustomNotes ? PitchedClip.Notes(step) : Voicing(step.Label);
        return PitchedClip.Create(this, step, notes, () => Render(notes));
    }

    public static string[] Voicing(string label) =>
        Voicings.TryGetValue(label, out var notes) ? notes : ["E4", "G4", "B4", "D5"];

    public static short[] Render(IReadOnlyList<string> notes)
    {
        var mix = new double[SynthMath.Length(1.05)];
        foreach (var note in notes)
            AddTine(mix, NoteMath.Frequency(note));

        for (var i = 0; i < mix.Length; i++)
        {
            var time = (double)i / ChordSynth.SampleRate;
            var tremolo = 0.74 + 0.26 * Math.Sin(2 * Math.PI * 5.1 * time);
            mix[i] *= tremolo;
        }

        return SynthMath.ToPcm(mix);
    }

    static void AddTine(double[] mix, double frequency)
    {
        SynthMath.AddSine(mix, frequency, 1, 0.85, attackSamples: 90);
        SynthMath.AddSine(mix, frequency * 2, 0.42, 0.42, attackSamples: 40);
        SynthMath.AddSine(mix, frequency * 3, 0.16, 0.24, attackSamples: 20);
        SynthMath.AddSine(mix, frequency * 4.003, 0.07, 0.14);
        SynthMath.AddNoise(mix, 0, 280, 0.08, SynthMath.Seed(frequency, 19), decaySamples: 40);
    }
}
