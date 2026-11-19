namespace BZPlaysFoolChords;

public sealed class AcousticGuitarInstrument : IInstrument
{
    public string Id => "acoustic";
    public string DisplayName => "Acoustic guitar";
    public IEnumerable<string> Aliases => ["guitar", "acousticguitar", "regularguitar"];

    public InstrumentClip? ClipFor(InstrumentRequest request)
    {
        var notes = PitchedClip.Notes(request.Step);
        return PitchedClip.Create(this, request.Step, notes, () => Render(notes));
    }

    public static short[] Render(IReadOnlyList<string> notes)
    {
        var mix = new double[SynthMath.Length(0.72)];
        foreach (var note in notes)
        {
            var frequency = NoteMath.Frequency(note);
            Pluck(mix, frequency, 1);
            Pluck(mix, frequency * 1.004, 2);
        }

        return SynthMath.ToPcm(mix);
    }

    static void Pluck(double[] mix, double frequency, int salt) =>
        SynthMath.Karplus(mix, frequency, sustainSeconds: 0.28, brightness: 0.22, SynthMath.Seed(frequency, salt), gain: 0.55);
}
