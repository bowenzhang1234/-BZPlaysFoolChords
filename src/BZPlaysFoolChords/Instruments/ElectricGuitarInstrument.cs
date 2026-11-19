namespace BZPlaysFoolChords;

public sealed class ElectricGuitarInstrument : IInstrument
{
    public string Id => "electric";
    public string DisplayName => "Electric guitar";
    public IEnumerable<string> Aliases => ["electricguitar"];

    public InstrumentClip? ClipFor(InstrumentRequest request)
    {
        var notes = PitchedClip.Notes(request.Step);
        return PitchedClip.Create(this, request.Step, notes, () => Render(notes));
    }

    public static short[] Render(IReadOnlyList<string> notes)
    {
        var dry = new double[SynthMath.Length()];
        foreach (var note in notes)
        {
            var frequency = NoteMath.Frequency(note);
            SynthMath.Karplus(dry, frequency, sustainSeconds: 1.15, brightness: 0.04, SynthMath.Seed(frequency, 11), gain: 0.7);
        }

        var driven = new double[dry.Length];
        var lowpass = 0.0;
        for (var i = 0; i < dry.Length; i++)
        {
            var clipped = Math.Tanh(3.4 * dry[i]);
            lowpass += 0.22 * (clipped - lowpass);
            driven[i] = lowpass;
        }

        return SynthMath.ToPcm(driven);
    }
}
